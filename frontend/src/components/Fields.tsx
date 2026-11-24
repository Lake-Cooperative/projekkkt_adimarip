import type { InputHTMLAttributes, ReactNode } from 'react';

type Option = { value: string | number; label: string };

type SelectProps = {
  label: string;
  value: string;
  options: Option[];
  onChange: (value: string) => void;
  /** Пустой вариант («Выберите…»); если не задан — пустого варианта нет. */
  placeholder?: string;
  required?: boolean;
  className?: string;
};

/** Подпись + выпадающий список. */
export function SelectField({ label, value, options, onChange, placeholder, required, className }: SelectProps) {
  return (
    <label className={className}>
      {label}
      <select required={required} value={value} onChange={e => onChange(e.target.value)}>
        {placeholder !== undefined && <option value="">{placeholder}</option>}
        {options.map(o => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </select>
    </label>
  );
}

type TextProps = Omit<InputHTMLAttributes<HTMLInputElement>, 'onChange' | 'value'> & {
  label: ReactNode;
  value: string;
  onChange: (value: string) => void;
  multiline?: boolean;
};

/** Подпись + поле ввода (или многострочное поле при `multiline`). */
export function TextField({ label, value, onChange, multiline, className, maxLength, required, ...input }: TextProps) {
  return (
    <label className={className}>
      {label}
      {multiline ? (
        <textarea required={required} maxLength={maxLength} value={value} onChange={e => onChange(e.target.value)} />
      ) : (
        <input
          {...input}
          required={required}
          maxLength={maxLength}
          value={value}
          onChange={e => onChange(e.target.value)}
        />
      )}
    </label>
  );
}
