import { readFileSync } from 'node:fs';

// The server sends CSP "script-src 'self'": a CDN or inline <script> here would be blocked
// (and would hand third parties code execution inside authenticated sessions).
test('index.html loads only the bundled entry script', () => {
  const html = readFileSync('index.html', 'utf8');
  const scripts = [...html.matchAll(/<script\b[^>]*>/g)].map(m => m[0]);
  expect(scripts).toEqual(['<script type="module" src="/src/main.tsx">']);
  expect(html).not.toMatch(/<style\b/);
});
