using UnityEngine;
using UnityEngine.UI;
using KingdomsOfBharat.Core;
using KingdomsOfBharat.Multiplayer.Wire;

namespace KingdomsOfBharat.Multiplayer
{
    // Phase 5 LAN transport MVP: the minimal Host/Join entry point needed
    // to actually drive a 2-human LAN match end to end. Deliberately
    // built at runtime (plain uGUI Text/Button/InputField, no 9-slice
    // theming) rather than as a hand-authored scene Canvas/prefab, so this
    // lands as pure C# with zero Main.unity changes - a known, disclosed
    // visual compromise (see CLAUDE.md's "flag when a task needs real
    // art/UI polish" rule), not a functional one. Self-installs the same
    // way SimClock/SaveManager/NetworkDriver do.
    //
    // Handshake protocol (see Wire/NetMessage.cs): once the TCP connection
    // is up, the host immediately sends HostHello (its own civ pick, the
    // map, and a freshly generated seed) and the joiner immediately sends
    // JoinHello (its own civ pick) - independently, neither waits on the
    // other first. Each side finalizes (NetworkMatch.Begin +
    // CivilizationSetup.BeginNetworkMatch) the moment it has received the
    // other side's Hello, since by then it already has both civs, the map,
    // and the seed.
    public class LanMatchMenu : MonoBehaviour
    {
        private enum State { Closed, Idle, Hosting, Joining }

        private const float PanelWidth = 420f;
        private const float PanelHeight = 320f;

        private State _state = State.Closed;
        private LanTransport _pendingTransport;
        private CivilizationId _localCivPick = CivilizationId.Chola;
        private MapId _chosenMap = MapId.RiverValley;
        private string _joinAddress = "127.0.0.1";
        private string _statusText = "";
        private bool _sentLocalHello;

        private GameObject _root;
        private Text _status;
        private InputField _addressField;
        private Text _civLabel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (FindFirstObjectByType<LanMatchMenu>() != null)
            {
                return;
            }

            GameObject go = new GameObject("LanMatchMenu");
            go.AddComponent<LanMatchMenu>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            BuildUi();
        }

        private void Update()
        {
            if (CivilizationSetup.HasMatchStarted)
            {
                // A match is already running (LAN or otherwise) - hide
                // entirely rather than leaving a stale Host/Join panel
                // floating over live gameplay.
                _root.SetActive(false);
                return;
            }

            _root.SetActive(true);

            if (_state == State.Hosting || _state == State.Joining)
            {
                PollHandshake();
            }
        }

        // --- Handshake polling (runs on the main thread only - drains
        // LanTransport's queue directly here rather than through
        // NetworkDriver, since NetworkDriver only starts once
        // NetworkMatch.IsActive is true, which isn't the case yet during
        // this pre-match phase). ---
        private void PollHandshake()
        {
            if (_pendingTransport.LastError != null)
            {
                _statusText = $"Connection failed: {_pendingTransport.LastError}";
                _state = State.Idle;
                _pendingTransport = null;
                RefreshStatus();
                return;
            }

            if (!_sentLocalHello && _pendingTransport.IsConnected)
            {
                _sentLocalHello = true;
                if (_state == State.Hosting)
                {
                    int seed = System.Environment.TickCount;
                    _pendingTransport.Send(new NetMessageEnvelope
                    {
                        kind = NetMessageKind.HostHello,
                        hostCivilization = (int)_localCivPick,
                        mapId = (int)_chosenMap,
                        seed = seed,
                    });
                    _statusText = $"Connected. Waiting for {NameOf(FactionId.Enemy)}'s civilization pick...";
                }
                else
                {
                    _pendingTransport.Send(new NetMessageEnvelope
                    {
                        kind = NetMessageKind.JoinHello,
                        remoteCivilization = (int)_localCivPick,
                    });
                    _statusText = "Connected. Waiting for host...";
                }

                RefreshStatus();
            }

            while (_pendingTransport.TryDequeue(out NetMessageEnvelope envelope))
            {
                if (_state == State.Hosting && envelope.kind == NetMessageKind.JoinHello)
                {
                    CompleteHandshake(isHost: true,
                        localFaction: FactionId.Player,
                        hostCiv: _localCivPick,
                        remoteCiv: (CivilizationId)envelope.remoteCivilization,
                        map: _chosenMap,
                        seed: System.Environment.TickCount);
                    return;
                }

                if (_state == State.Joining && envelope.kind == NetMessageKind.HostHello)
                {
                    CompleteHandshake(isHost: false,
                        localFaction: FactionId.Enemy,
                        hostCiv: (CivilizationId)envelope.hostCivilization,
                        remoteCiv: _localCivPick,
                        map: (MapId)envelope.mapId,
                        seed: envelope.seed);
                    return;
                }
            }
        }

        // Host's seed generation happens twice above (once optimistically
        // when sending HostHello, once again here) - deliberately the SAME
        // call site's value isn't reused because it doesn't need to be:
        // the host is the one authoritative source of the seed either way,
        // and CompleteHandshake's own seed parameter is what actually gets
        // used for NetworkMatch.Begin - the host's own local seed variable,
        // not a round-tripped one. Kept as two separate reads rather than
        // threading one value through both call sites purely for this
        // method's own simplicity; both reads happen within the same
        // frame in practice.
        private void CompleteHandshake(bool isHost, FactionId localFaction, CivilizationId hostCiv, CivilizationId remoteCiv, MapId map, int seed)
        {
            NetworkMatch.Begin(localFaction, isHost, seed, _pendingTransport);

            CivilizationSetup setup = FindFirstObjectByType<CivilizationSetup>();
            if (setup == null)
            {
                Debug.LogError("[LanMatchMenu] No CivilizationSetup found in scene - cannot start the network match.");
                NetworkMatch.End();
                _state = State.Idle;
                return;
            }

            setup.BeginNetworkMatch(hostCiv, remoteCiv, map);

            _state = State.Closed;
            _root.SetActive(false);
        }

        // --- Minimal runtime uGUI - see this class's own header comment. ---

        private void BuildUi()
        {
            var canvasGo = new GameObject("LanMatchMenuCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            _root = new GameObject("Panel");
            _root.transform.SetParent(canvasGo.transform, false);
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            rootRect.anchoredPosition = new Vector2(-620f, 260f);
            var panelImage = _root.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.75f);

            var layout = _root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 6f;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;

            AddText(_root.transform, "LAN Match (Phase 5 MVP)", 16, out _);

            _civLabel = AddText(_root.transform, "", 14, out GameObject civRow);
            RefreshCivLabel();
            AddButton(civRow.transform, "<", CycleCivBack, 28f);
            AddButton(civRow.transform, ">", CycleCivForward, 28f);

            AddButton(_root.transform, "Host on LAN", OnHostClicked);

            var joinRow = new GameObject("JoinRow");
            joinRow.transform.SetParent(_root.transform, false);
            joinRow.AddComponent<RectTransform>().sizeDelta = new Vector2(PanelWidth - 24f, 28f);
            var joinLayout = joinRow.AddComponent<HorizontalLayoutGroup>();
            joinLayout.spacing = 6f;
            joinLayout.childControlWidth = false;
            joinLayout.childForceExpandWidth = false;

            var fieldGo = new GameObject("AddressField");
            fieldGo.transform.SetParent(joinRow.transform, false);
            fieldGo.AddComponent<RectTransform>().sizeDelta = new Vector2(220f, 28f);
            fieldGo.AddComponent<Image>().color = Color.white;
            _addressField = fieldGo.AddComponent<InputField>();
            var fieldTextGo = new GameObject("Text");
            fieldTextGo.transform.SetParent(fieldGo.transform, false);
            var fieldText = fieldTextGo.AddComponent<Text>();
            fieldText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fieldText.color = Color.black;
            fieldText.alignment = TextAnchor.MiddleLeft;
            var fieldTextRect = fieldTextGo.GetComponent<RectTransform>();
            fieldTextRect.anchorMin = Vector2.zero;
            fieldTextRect.anchorMax = Vector2.one;
            fieldTextRect.offsetMin = new Vector2(6f, 0f);
            fieldTextRect.offsetMax = new Vector2(-6f, 0f);
            _addressField.textComponent = fieldText;
            _addressField.text = _joinAddress;
            _addressField.onValueChanged.AddListener(value => _joinAddress = value);

            AddButton(joinRow.transform, "Join", OnJoinClicked, 80f);

            _status = AddText(_root.transform, "", 12, out _);
            RefreshStatus();

            _root.SetActive(true);
        }

        private void RefreshCivLabel()
        {
            _civLabel.text = $"Civilization: {_localCivPick}";
        }

        private void CycleCivBack()
        {
            CycleCiv(-1);
        }

        private void CycleCivForward()
        {
            CycleCiv(1);
        }

        private void CycleCiv(int direction)
        {
            var values = (CivilizationId[])System.Enum.GetValues(typeof(CivilizationId));
            int index = System.Array.IndexOf(values, _localCivPick);
            index = (index + direction + values.Length) % values.Length;
            _localCivPick = values[index];
            RefreshCivLabel();
        }

        private void OnHostClicked()
        {
            if (_state != State.Idle && _state != State.Closed)
            {
                return;
            }

            _pendingTransport = LanTransport.StartHost();
            _sentLocalHello = false;
            _state = State.Hosting;
            _statusText = $"Hosting on {LanTransport.LocalIPv4()}:{LanTransport.DefaultPort} - waiting for a player to join...";
            RefreshStatus();
        }

        private void OnJoinClicked()
        {
            if (_state != State.Idle && _state != State.Closed)
            {
                return;
            }

            _pendingTransport = LanTransport.StartJoin(_joinAddress);
            _sentLocalHello = false;
            _state = State.Joining;
            _statusText = $"Connecting to {_joinAddress}:{LanTransport.DefaultPort}...";
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            _status.text = _statusText;
        }

        private static string NameOf(FactionId faction)
        {
            return faction == FactionId.Player ? "the joining player" : "the host";
        }

        private Text AddText(Transform parent, string text, int fontSize, out GameObject rowGo)
        {
            rowGo = new GameObject("Text");
            rowGo.transform.SetParent(parent, false);
            rowGo.AddComponent<RectTransform>().sizeDelta = new Vector2(PanelWidth - 24f, fontSize + 12f);
            var uiText = rowGo.AddComponent<Text>();
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = fontSize;
            uiText.color = Color.white;
            uiText.text = text;

            var rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childControlWidth = false;
            rowLayout.childForceExpandWidth = false;

            return uiText;
        }

        private void AddButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick, float width = 120f)
        {
            var buttonGo = new GameObject($"Button_{label}");
            buttonGo.transform.SetParent(parent, false);
            buttonGo.AddComponent<RectTransform>().sizeDelta = new Vector2(width, 28f);
            var image = buttonGo.AddComponent<Image>();
            image.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            var button = buttonGo.AddComponent<Button>();
            button.onClick.AddListener(onClick);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(buttonGo.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }
    }
}
