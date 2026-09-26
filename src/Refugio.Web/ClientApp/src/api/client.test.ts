import { describe, it, expect } from 'vitest';
import { qs } from './client';

describe('qs', () => {
  it('builds a query string from defined values', () => {
    expect(qs({ search: 'fido', page: 2 })).toBe('?search=fido&page=2');
  });

  it('skips undefined, null and empty-string values', () => {
    expect(qs({ search: undefined, status: null, empty: '', page: 1 })).toBe('?page=1');
  });

  it('returns empty string when nothing to encode', () => {
    expect(qs({ search: undefined })).toBe('');
  });

  it('url-encodes values', () => {
    expect(qs({ search: 'a b&c' })).toBe('?search=a%20b%26c');
  });
});
