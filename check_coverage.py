#!/usr/bin/env python
"""
Coverage checker for rpyforth against Forth 2012 Core wordset.

This script analyzes the rpyforth implementation to determine which
Forth 2012 Core words are implemented and generates coverage reports.
"""

import re
import os
from collections import defaultdict

# Forth 2012 Core wordset (133 words) - Official ANS Forth 2012 standard
# Source: https://forth-standard.org/standard/core
FORTH_2012_CORE_WORDS = {
    # Stack manipulation
    "DUP", "DROP", "SWAP", "OVER", "ROT", "?DUP", "PICK",
    "2DUP", "2DROP", "2SWAP", "2OVER",
    ">R", "R>", "R@", "2>R", "2R>", "2R@",

    # Arithmetic
    "+", "-", "*", "/", "MOD", "/MOD", "*/", "*/MOD",
    "1+", "1-", "2*", "2/",
    "ABS", "NEGATE", "MAX", "MIN",
    "M*", "UM*", "FM/MOD", "SM/REM", "UM/MOD",

    # Comparison
    "=", "<", ">", "U<",
    "0=", "0<", "0>",

    # Logical/Bitwise
    "AND", "OR", "XOR", "INVERT",
    "LSHIFT", "RSHIFT",

    # Memory
    "!", "@", "C!", "C@", "+!",
    "2!", "2@",
    "CELL+", "CELLS", "CHAR+", "CHARS",
    "ALIGNED", "ALIGN",

    # Data space
    "HERE", "ALLOT", ",", "C,",

    # Compilation
    ":", ";", "IMMEDIATE", "RECURSE",
    "[", "]", "LITERAL", "[']", "[CHAR]",
    "POSTPONE", "DOES>",

    # Control structures
    "IF", "ELSE", "THEN",
    "BEGIN", "WHILE", "REPEAT", "UNTIL", "AGAIN",
    "DO", "LOOP", "+LOOP", "UNLOOP", "LEAVE",
    "I", "J",
    "EXIT",

    # Constants
    "BL",

    # Conversion
    "S>D",

    # Variables and constants
    "VARIABLE", "CONSTANT", "CREATE",

    # Dictionary
    "FIND", "EXECUTE", ">BODY",

    # I/O
    "EMIT", "TYPE", "CR", "SPACE", "SPACES",
    ".", "U.",
    ".\"", "S\"",
    "KEY", "ACCEPT",

    # Number conversion
    ">NUMBER", "BASE",

    # Parsing
    "WORD", "CHAR", "COUNT",

    # Source
    "SOURCE", ">IN",

    # Pictured numeric output
    "<#", "#", "#S", "#>", "HOLD", "SIGN",

    # Misc
    "STATE", "QUIT", "ABORT", "ABORT\"",
    "EVALUATE", "ENVIRONMENT?",
    "FILL", "MOVE",
    "DECIMAL", "HEX",

    # Special characters
    "'", "(", "DEPTH"
}


def extract_primitives_from_file(filepath):
    """Extract primitive word names from primitives.py"""
    words = set()

    with open(filepath, 'r') as f:
        content = f.read()

        # Find all define_prim calls
        pattern = r'outer\.define_prim\("([^"]+)"'
        matches = re.findall(pattern, content)
        words.update(matches)

    return words


def extract_special_words_from_outer(filepath):
    """Extract special compilation words from outer_interp.py"""
    words = set()

    with open(filepath, 'r') as f:
        content = f.read()

        # Look for special word handlers
        # Pattern: if tkey == "WORD":
        pattern = r'if\s+tkey\s*==\s*"([^"]+)"'
        matches = re.findall(pattern, content)
        words.update(matches)

        # Pattern: tkey == "WORD" (in OR conditions)
        pattern = r'tkey\s*==\s*"([^"]+)"'
        matches = re.findall(pattern, content)
        words.update(matches)

        # Look for if t == 'WORD': (single quotes)
        # This will capture things like S", .", etc. which may contain special chars
        pattern = r"if\s+t\s*==\s*'([^']+)'"
        matches = re.findall(pattern, content)
        words.update(matches)

        # Look for if t == "WORD": (double quotes)
        pattern = r'if\s+t\s*==\s*"([^"]+)"'
        matches = re.findall(pattern, content)
        words.update(matches)

    return words


def extract_colon_definitions():
    """Manually specified colon definitions and syntactic elements"""
    # These are words that are implemented but may not be detected by regex
    return {
        ":", ";",  # Colon definition syntax
    }


def categorize_words():
    """Categorize Forth 2012 Core words by functionality"""
    categories = {
        "Stack Manipulation": {
            "DUP", "DROP", "SWAP", "OVER", "ROT", "?DUP", "PICK",
            "2DUP", "2DROP", "2SWAP", "2OVER"
        },
        "Return Stack": {
            ">R", "R>", "R@", "2>R", "2R>", "2R@"
        },
        "Arithmetic": {
            "+", "-", "*", "/", "MOD", "/MOD", "*/", "*/MOD",
            "1+", "1-", "2*", "2/",
            "ABS", "NEGATE", "MAX", "MIN",
            "M*", "UM*", "FM/MOD", "SM/REM", "UM/MOD"
        },
        "Comparison": {
            "=", "<", ">", "U<",
            "0=", "0<", "0>", "0<>"
        },
        "Logical & Bitwise": {
            "AND", "OR", "XOR", "INVERT",
            "LSHIFT", "RSHIFT"
        },
        "Memory Access": {
            "!", "@", "C!", "C@", "+!",
            "2!", "2@",
            "CELL+", "CELLS", "CHAR+", "CHARS",
            "ALIGNED", "ALIGN"
        },
        "Data Space": {
            "HERE", "ALLOT", ",", "C,"
        },
        "Compilation": {
            ":", ";", "IMMEDIATE", "RECURSE",
            "[", "]", "LITERAL", "[']", "[CHAR]",
            "POSTPONE", "DOES>"
        },
        "Control Flow": {
            "IF", "ELSE", "THEN",
            "BEGIN", "WHILE", "REPEAT", "UNTIL", "AGAIN",
            "DO", "LOOP", "+LOOP", "UNLOOP", "LEAVE",
            "I", "J",
            "EXIT"
        },
        "Variables & Constants": {
            "VARIABLE", "CONSTANT", "CREATE"
        },
        "Dictionary": {
            "FIND", "EXECUTE", ">BODY"
        },
        "I/O": {
            "EMIT", "TYPE", "CR", "SPACE", "SPACES",
            ".", "U.",
            ".\"", "S\"",
            "KEY", "ACCEPT"
        },
        "Number Conversion": {
            ">NUMBER", "BASE", "DECIMAL", "HEX"
        },
        "Parsing": {
            "WORD", "CHAR", "COUNT"
        },
        "Source Input": {
            "SOURCE", ">IN"
        },
        "Pictured Numeric Output": {
            "<#", "#", "#S", "#>", "HOLD", "SIGN"
        },
        "System": {
            "STATE", "QUIT", "ABORT", "ABORT\"",
            "EVALUATE", "ENVIRONMENT?",
            "FILL", "MOVE",
            "DEPTH", "BL", "S>D"
        },
        "Special": {
            "'", "("
        }
    }
    return categories


def generate_report(implemented, missing, categories):
    """Generate a detailed coverage report"""

    total = len(FORTH_2012_CORE_WORDS)
    impl_count = len(implemented)
    miss_count = len(missing)
    coverage_pct = (impl_count / total) * 100 if total > 0 else 0

    report = []
    report.append("=" * 80)
    report.append("RPyForth - Forth 2012 Core Wordset Coverage Report")
    report.append("=" * 80)
    report.append("")
    report.append(f"Total Forth 2012 Core words: {total}")
    report.append(f"Implemented: {impl_count} ({coverage_pct:.1f}%)")
    report.append(f"Missing: {miss_count} ({100-coverage_pct:.1f}%)")
    report.append("")

    # Coverage by category
    report.append("=" * 80)
    report.append("Coverage by Category")
    report.append("=" * 80)
    report.append("")

    for category, words in sorted(categories.items()):
        cat_total = len(words)
        cat_impl = len(words & implemented)
        cat_miss = cat_total - cat_impl
        cat_pct = (cat_impl / cat_total * 100) if cat_total > 0 else 0

        report.append(f"{category}:")
        report.append(f"  Total: {cat_total}, Implemented: {cat_impl}, Missing: {cat_miss} ({cat_pct:.0f}% coverage)")

        if cat_impl > 0:
            report.append(f"  Implemented: {', '.join(sorted(words & implemented))}")
        if cat_miss > 0:
            report.append(f"  Missing: {', '.join(sorted(words & missing))}")
        report.append("")

    # Detailed lists
    report.append("=" * 80)
    report.append("Implemented Words (Alphabetical)")
    report.append("=" * 80)
    report.append("")

    impl_list = sorted(implemented)
    for i in range(0, len(impl_list), 10):
        report.append("  " + ", ".join(impl_list[i:i+10]))
    report.append("")

    report.append("=" * 80)
    report.append("Missing Words (Alphabetical)")
    report.append("=" * 80)
    report.append("")

    miss_list = sorted(missing)
    for i in range(0, len(miss_list), 10):
        report.append("  " + ", ".join(miss_list[i:i+10]))
    report.append("")

    # Critical missing words
    critical = {
        "Memory": {"C!", "C@", "+!"},
        "Arithmetic": {"/", "/MOD", "*/", "*/MOD", "2*", "2/", "UM*", "FM/MOD", "SM/REM", "UM/MOD"},
        "Return Stack": {">R", "R>", "R@"},
        "Control Flow": {"UNTIL", "AGAIN", "+LOOP", "UNLOOP"},
        "Compilation": {"IMMEDIATE", "RECURSE", "[", "]", "LITERAL", "[']", "POSTPONE", "DOES>"},
        "Dictionary": {"FIND", "EXECUTE", ">BODY", "CREATE"},
        "Data Space": {"HERE", "ALLOT", ",", "C,"},
        "I/O": {"CR", "SPACE", "SPACES", "U.", "KEY", "ACCEPT"},
        "System": {"STATE", "QUIT", "ABORT", "ABORT\"", "EVALUATE", "ENVIRONMENT?"},
        "Parsing": {"WORD", "COUNT", "SOURCE", ">IN", "'", "("},
    }

    report.append("=" * 80)
    report.append("Critical Missing Words (Grouped by Importance)")
    report.append("=" * 80)
    report.append("")

    for cat_name, words in critical.items():
        missing_critical = words & missing
        if missing_critical:
            report.append(f"{cat_name}: {', '.join(sorted(missing_critical))}")

    report.append("")

    return "\n".join(report)


def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    primitives_path = os.path.join(script_dir, "rpyforth", "primitives.py")
    outer_path = os.path.join(script_dir, "rpyforth", "outer_interp.py")

    print("Analyzing rpyforth implementation...")
    print()

    # Extract implemented words
    prim_words = extract_primitives_from_file(primitives_path)
    print(f"Found {len(prim_words)} primitive words in primitives.py")

    special_words = extract_special_words_from_outer(outer_path)
    print(f"Found {len(special_words)} special words in outer_interp.py")

    colon_defs = extract_colon_definitions()
    print(f"Added {len(colon_defs)} colon definition words")

    # Combine all implemented words (uppercase for comparison)
    implemented = set()
    for word in prim_words | special_words | colon_defs:
        implemented.add(word.upper())

    print(f"\nTotal unique implemented words: {len(implemented)}")
    print()

    # Compare with Forth 2012 Core
    missing = FORTH_2012_CORE_WORDS - implemented

    # Also check for implemented words not in Forth 2012 Core (extensions)
    extensions = implemented - FORTH_2012_CORE_WORDS

    print(f"Forth 2012 Core words implemented: {len(implemented & FORTH_2012_CORE_WORDS)}")
    print(f"Forth 2012 Core words missing: {len(missing)}")
    print(f"Extension words (not in Core): {len(extensions)}")
    if extensions:
        print(f"  Extensions: {', '.join(sorted(extensions))}")
    print()

    # Generate categorized report
    categories = categorize_words()
    report = generate_report(implemented & FORTH_2012_CORE_WORDS, missing, categories)

    # Write report to file
    report_path = os.path.join(script_dir, "FORTH2012_COVERAGE.md")
    with open(report_path, 'w') as f:
        f.write(report)

    print(f"Coverage report written to: {report_path}")
    print()

    # Print summary to console
    print("=" * 80)
    print("SUMMARY")
    print("=" * 80)
    total = len(FORTH_2012_CORE_WORDS)
    impl_count = len(implemented & FORTH_2012_CORE_WORDS)
    coverage_pct = (impl_count / total) * 100
    print(f"Coverage: {impl_count}/{total} words ({coverage_pct:.1f}%)")
    print()

    # Print a quick breakdown
    print("Quick breakdown by category:")
    for category, words in sorted(categories.items()):
        cat_total = len(words)
        cat_impl = len(words & implemented)
        cat_pct = (cat_impl / cat_total * 100) if cat_total > 0 else 0
        bar_length = 30
        bar_filled = int(bar_length * cat_impl / cat_total) if cat_total > 0 else 0
        bar = "█" * bar_filled + "░" * (bar_length - bar_filled)
        print(f"  {category:25s} [{bar}] {cat_pct:5.1f}% ({cat_impl}/{cat_total})")


if __name__ == "__main__":
    main()
