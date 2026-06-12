interface Props { message: string; }

export function ErrorMessage({ message }: Props) {
  return (
    <div className="bg-error-container/30 border border-error/30 rounded-xl px-md py-3 text-body-sm text-on-error-container">
      <span className="material-symbols-outlined align-middle mr-1" style={{ fontSize: 16 }}>error</span>
      {message}
    </div>
  );
}
