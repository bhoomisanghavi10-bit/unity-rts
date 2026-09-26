#!/usr/bin/env python3
"""Compare the validation logs written by two LAN peers.

Each peer is started with `-lanlog <path>` (or KOB_LAN_LOG=<path>); see
docs/LAN_TWO_INSTANCE_VALIDATION.md. Lines:
  CFG <configHash> seed <seed> map <map> local <faction>
  CMD <tick> <faction> <seq> <CommandType>     (one per executed command)
  HASH <tick> <stateHash>                      (every 20th tick)
Exit code 0 = the peers agree on everything both recorded, 1 otherwise.
"""
import sys


def load(path):
    cfg, cmds, hashes = None, [], {}
    with open(path) as f:
        for line in f:
            p = line.split()
            if not p:
                continue
            if p[0] == "CFG":
                cfg = (p[1], p[3], p[5])  # hash, seed, map (local faction differs by design)
            elif p[0] == "CMD":
                cmds.append((int(p[1]), int(p[2]), int(p[3]), p[4]))
            elif p[0] == "HASH":
                hashes[int(p[1])] = p[2]
    return cfg, cmds, hashes


def main(a_path, b_path):
    a, b = load(a_path), load(b_path)
    ok = True
    if a[0] != b[0]:
        print(f"CONFIG MISMATCH: {a[0]} vs {b[0]}")
        ok = False
    else:
        print(f"config OK: {a[0]}")

    # Commands compared in canonical order over the ticks both peers reached.
    last = min(max((c[0] for c in a[1]), default=-1), max((c[0] for c in b[1]), default=-1))
    ca = sorted(c for c in a[1] if c[0] <= last)
    cb = sorted(c for c in b[1] if c[0] <= last)
    if ca != cb:
        ok = False
        only_a = [c for c in ca if c not in cb][:5]
        only_b = [c for c in cb if c not in ca][:5]
        print(f"COMMAND LOG MISMATCH up to tick {last}: only in A {only_a}, only in B {only_b}")
    else:
        print(f"command logs OK: {len(ca)} commands up to tick {last}")

    common = sorted(set(a[2]) & set(b[2]))
    bad = [t for t in common if a[2][t] != b[2][t]]
    if bad:
        ok = False
        print(f"STATE HASH DIVERGED at ticks {bad[:5]} (first: {bad[0]}: {a[2][bad[0]]} vs {b[2][bad[0]]})")
    else:
        print(f"state hashes OK: {len(common)} shared checkpoints")
    return 0 if ok else 1


if __name__ == "__main__":
    if len(sys.argv) != 3:
        print(__doc__)
        sys.exit(2)
    sys.exit(main(sys.argv[1], sys.argv[2]))
