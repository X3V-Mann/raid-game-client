export default function RubleIcon({ size = 16, alt = "руб.", className = "", style }) {
  return (
    <img
      src="/icons/ruble.jpg"
      alt={alt}
      className={`ruble-icon ${className}`.trim()}
      style={{ width: size, height: size, verticalAlign: "middle", display: "inline-block", objectFit: "contain", ...style }}
    />
  );
}