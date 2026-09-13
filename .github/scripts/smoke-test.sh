set -uo pipefail

BASE_URL="${1:?usage: smoke-test.sh <base-url>}"
BASE_URL="${BASE_URL%/}"

# endpoint | attempts | delay-seconds
check() {
  local path="$1" attempts="$2" delay="$3"
  local url="${BASE_URL}${path}"
  local body_file code

  body_file="$(mktemp)"
  echo "==> ${url} (up to ${attempts} attempts, ${delay}s apart)"

  for ((i = 1; i <= attempts; i++)); do
    code="$(curl -sS -o "${body_file}" -w '%{http_code}' --max-time 25 "${url}" || echo 000)"
    if [[ "${code}" == "200" ]]; then
      echo "    attempt ${i}: ${code} OK"
      rm -f "${body_file}"
      return 0
    fi
    echo "    attempt ${i}: ${code} - retrying in ${delay}s"
    sleep "${delay}"
  done

  echo "::error::Smoke test failed for ${url} (last status ${code})"
  echo "--- last response body ---"
  cat "${body_file}" || true
  echo
  echo "--------------------------"
  rm -f "${body_file}"
  return 1
}

check "/api/health/live"  10 10 || exit 1
check "/api/health/ready" 20 15 || exit 1

echo "Smoke test passed."
