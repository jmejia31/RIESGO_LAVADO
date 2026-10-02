const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const ExcelJS = require(path.join(__dirname, '..', 'frontend', 'rl-app', 'node_modules', 'exceljs'));

const EXPECTED_SHA256 = '5c3fc00864947afe1e34d3d6ffdfc6da008eaa3c8f1c6c764161014d5ef9a385';

async function exportExcel() {
  const repoRoot = path.resolve(__dirname, '..');
  const excelPath = path.join(repoRoot, 'Matrices de Riesgos.xlsx');

  const fileBuffer = fs.readFileSync(excelPath);
  const actualHash = crypto.createHash('sha256').update(fileBuffer).digest('hex').toLowerCase();

  if (actualHash !== EXPECTED_SHA256) {
    console.error(`ERROR: SHA256 mismatch. Expected ${EXPECTED_SHA256}, got ${actualHash}`);
    process.exit(1);
  }

  const wb = new ExcelJS.Workbook();
  await wb.xlsx.load(fileBuffer);
  const ws = wb.getWorksheet('Matriz Consolidada');
  if (!ws) {
    console.error('ERROR: Worksheet "Matriz Consolidada" not found.');
    process.exit(1);
  }

  const headers = [];
  const headerRow = ws.getRow(1);
  for (let c = 1; c <= 82; c++) {
    const val = headerRow.getCell(c).value;
    headers.push(val != null ? String(val).trim() : '');
  }

  const matrix = [];

  for (let r = 2; r <= 60; r++) {
    const row = ws.getRow(r);
    const riskNo = Number(row.getCell(1).value);
    const rawCode = row.getCell(2).value;
    const riskCode = rawCode != null ? String(rawCode).trim() : '';

    const rowCells = [];
    for (let c = 1; c <= 82; c++) {
      const cell = row.getCell(c);
      let formula = null;
      let cachedResult = null;
      let rawValue = cell.value;
      let textValue = '';

      if (rawValue !== null && rawValue !== undefined) {
        if (typeof rawValue === 'object') {
          if (rawValue.formula) {
            formula = rawValue.formula;
            cachedResult = rawValue.result !== undefined ? rawValue.result : null;
          }
          if (rawValue.text !== undefined) {
            textValue = String(rawValue.text);
          } else if (rawValue.richText && Array.isArray(rawValue.richText)) {
            textValue = rawValue.richText.map(t => t.text || '').join('');
          } else if (cachedResult !== null) {
            textValue = String(cachedResult);
          } else {
            textValue = '';
          }
        } else {
          textValue = String(rawValue);
        }
      }

      rowCells.push({
        row: r,
        col: c,
        fieldNumber: c,
        fieldLabel: headers[c - 1],
        formula: formula,
        cachedResult: cachedResult,
        rawValue: rawValue,
        textValue: textValue
      });
    }

    matrix.push({
      row: r,
      riskNo: riskNo,
      riskCode: riskCode,
      cells: rowCells
    });
  }

  const outputPath = path.join(repoRoot, 'scratch_excel_59x82.json');
  fs.writeFileSync(outputPath, JSON.stringify({
    sourceHash: actualHash,
    sheet: 'Matriz Consolidada',
    headers: headers,
    riskCount: matrix.length,
    fieldCount: 82,
    matrix: matrix
  }, null, 2), 'utf8');

  console.log(`EXPORT_SUCCESS: 59 risks x 82 fields saved to ${outputPath}`);
}

exportExcel().catch(err => {
  console.error('FATAL:', err);
  process.exit(1);
});
