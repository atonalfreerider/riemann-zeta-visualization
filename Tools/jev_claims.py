#!/usr/bin/env python3
"""Check written claims against the evidence they rest on, with TypeSafe's System One model Jev (the citation-check
pattern, https://docs.typesafe.ai/cookbooks/citation_check). One Choice question per claim, all over the same state (the
evidence text), asked together: does the evidence support the claim, contradict it, or say nothing about it? A verdict
below the confidence threshold, or anything but "supports", goes to a person.

Run:  TYPESAFE_API_KEY=… python3 Tools/jev_claims.py EVIDENCE.txt CLAIMS.json   (CLAIMS.json: a list of strings)
"""
import json, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from jev_check import ask

AUTO_ACCEPT = 0.8
OPTIONS = {
    "supports": "The evidence states the claim or directly implies that it is true, including its numbers",
    "contradicts": "The evidence states something incompatible with the claim, such as a different number or the opposite conclusion",
    "says_nothing": "The evidence does not address what the claim asserts, either way",
}


def main():
    evidence = open(sys.argv[1], encoding="utf-8").read()
    claims = json.load(open(sys.argv[2], encoding="utf-8"))
    questions = {f"c{i}": {"type": "choice", "instructions": [f"Claim: {c}", "How does `evidence` relate to the claim?"], "criteria": OPTIONS}
                 for i, c in enumerate(claims)}
    res = ask({"evidence": evidence}, questions)
    review = 0
    for i, c in enumerate(claims):
        a = res["answers"][f"c{i}"]
        ok = a["choice"] == "supports" and a["confidence"] >= AUTO_ACCEPT
        review += not ok
        print(f"{'ok    ' if ok else 'REVIEW'} {a['choice']:12} {a['confidence']:.2f}  {c}")
    print(f"{len(claims) - review} accepted, {review} for review;  tokens {res.get('usage', {})}")


if __name__ == "__main__":
    main()
