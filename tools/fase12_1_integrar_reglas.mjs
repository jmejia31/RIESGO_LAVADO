import fs from 'node:fs';
import path from 'node:path';

const root = process.cwd();
const componentPath = path.join(root, 'frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.ts');
const templatePath = path.join(root, 'frontend/rl-app/src/app/features/admin/matrices-riesgos/pages/matrices-riesgos/matrices-riesgos.component.html');

function replace