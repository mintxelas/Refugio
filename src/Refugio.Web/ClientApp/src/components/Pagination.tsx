import { useSearchParams } from 'react-router-dom';

interface Props {
  totalCount: number;
  pageSize: number;
  pageNumber: number;
}

export function Pagination({ totalCount, pageSize, pageNumber }: Props) {
  const [searchParams, setSearchParams] = useSearchParams();
  const totalPages = Math.ceil(totalCount / pageSize);
  if (totalPages <= 1) return null;

  const go = (p: number) => {
    const next = new URLSearchParams(searchParams);
    next.set('page', String(p));
    setSearchParams(next, { replace: true });
  };

  return (
    <div className="flex items-center gap-1 mt-md">
      <button
        disabled={pageNumber <= 1}
        onClick={() => go(pageNumber - 1)}
        className="px-3 py-1.5 rounded-lg text-body-sm border border-outline-variant disabled:opacity-40 hover:bg-surface-container transition-all"
      >
        <span className="material-symbols-outlined" style={{ fontSize: 16 }}>chevron_left</span>
      </button>
      {Array.from({ length: totalPages }, (_, i) => i + 1).map(p => (
        <button
          key={p}
          onClick={() => go(p)}
          className={`px-3 py-1.5 rounded-lg text-body-sm border transition-all ${
            p === pageNumber
              ? 'bg-primary text-on-primary border-primary'
              : 'border-outline-variant hover:bg-surface-container'
          }`}
        >
          {p}
        </button>
      ))}
      <button
        disabled={pageNumber >= totalPages}
        onClick={() => go(pageNumber + 1)}
        className="px-3 py-1.5 rounded-lg text-body-sm border border-outline-variant disabled:opacity-40 hover:bg-surface-container transition-all"
      >
        <span className="material-symbols-outlined" style={{ fontSize: 16 }}>chevron_right</span>
      </button>
      <span className="text-body-sm text-on-surface-variant ml-2">
        {totalCount} en total
      </span>
    </div>
  );
}
