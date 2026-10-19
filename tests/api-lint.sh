#!/usr/bin/env bash
set -euo pipefail
# Run after Docker demo boot, or with ADIMARIP_API pointed at a test API.
python3 tests/api_acceptance.py
