#!/usr/bin/env bash
set -euo pipefail

echo "=========================================================="
echo "Running Unit Tests and Code Coverage for JTStressTests"
echo "=========================================================="

REPORT_DIR="${1:-./TestResults}"
mkdir -p "${REPORT_DIR}"

dotnet test JTStressTests.Tests/JTStressTests.Tests.csproj \
    --configuration Release \
    --logger "trx;LogFileName=test_results.trx" \
    --results-directory "${REPORT_DIR}" \
    --collect:"XPlat Code Coverage" \
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura,opencover

echo ""
echo "Tests completed successfully!"
echo "Test results and coverage reports stored in: ${REPORT_DIR}"
