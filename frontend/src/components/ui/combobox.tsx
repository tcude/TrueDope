import * as React from 'react';
import { cn } from '../../lib/utils';

interface ComboboxProps {
  value: string;
  onChange: (value: string) => void;
  options: string[];
  placeholder?: string;
  allowCustom?: boolean;
  error?: boolean;
  disabled?: boolean;
  className?: string;
  id?: string;
}

export function Combobox({
  value,
  onChange,
  options,
  placeholder = 'Select or type...',
  allowCustom = true,
  error = false,
  disabled = false,
  className,
  id,
}: ComboboxProps) {
  const [isOpen, setIsOpen] = React.useState(false);
  const [inputValue, setInputValue] = React.useState(value);
  const [highlightedIndex, setHighlightedIndex] = React.useState(-1);
  const containerRef = React.useRef<HTMLDivElement>(null);
  const inputRef = React.useRef<HTMLInputElement>(null);
  const listRef = React.useRef<HTMLUListElement>(null);

  // Filter options based on input
  const filteredOptions = React.useMemo(() => {
    if (!inputValue) return options;
    const lower = inputValue.toLowerCase();
    return options.filter((opt) => opt.toLowerCase().includes(lower));
  }, [options, inputValue]);

  // Sync input value with prop value
  React.useEffect(() => {
    setInputValue(value);
  }, [value]);

  // Close dropdown when clicking outside
  React.useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
        // Reset to actual value if user didn't select
        if (inputValue !== value) {
          if (allowCustom) {
            onChange(inputValue);
          } else {
            setInputValue(value);
          }
        }
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [inputValue, value, onChange, allowCustom]);

  // Scroll highlighted option into view
  React.useEffect(() => {
    if (highlightedIndex >= 0 && listRef.current) {
      const item = listRef.current.children[highlightedIndex] as HTMLElement;
      item?.scrollIntoView({ block: 'nearest' });
    }
  }, [highlightedIndex]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.value;
    setInputValue(newValue);
    setIsOpen(true);
    setHighlightedIndex(-1);
  };

  const handleSelect = (option: string) => {
    setInputValue(option);
    onChange(option);
    setIsOpen(false);
    inputRef.current?.focus();
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (disabled) return;

    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault();
        setIsOpen(true);
        setHighlightedIndex((prev) =>
          prev < filteredOptions.length - 1 ? prev + 1 : prev
        );
        break;
      case 'ArrowUp':
        e.preventDefault();
        setHighlightedIndex((prev) => (prev > 0 ? prev - 1 : -1));
        break;
      case 'Enter':
        e.preventDefault();
        if (highlightedIndex >= 0 && filteredOptions[highlightedIndex]) {
          handleSelect(filteredOptions[highlightedIndex]);
        } else if (allowCustom && inputValue) {
          onChange(inputValue);
          setIsOpen(false);
        }
        break;
      case 'Escape':
        setIsOpen(false);
        setInputValue(value);
        break;
      case 'Tab':
        if (isOpen && highlightedIndex >= 0 && filteredOptions[highlightedIndex]) {
          handleSelect(filteredOptions[highlightedIndex]);
        } else if (allowCustom && inputValue && inputValue !== value) {
          onChange(inputValue);
        }
        setIsOpen(false);
        break;
    }
  };

  const showDropdown = isOpen && (filteredOptions.length > 0 || (allowCustom && inputValue && !options.includes(inputValue)));

  return (
    <div ref={containerRef} className={cn('relative', className)}>
      <div className="relative">
        <input
          ref={inputRef}
          id={id}
          type="text"
          value={inputValue}
          onChange={handleInputChange}
          onFocus={() => setIsOpen(true)}
          onKeyDown={handleKeyDown}
          placeholder={placeholder}
          disabled={disabled}
          className={cn(
            'flex h-10 w-full rounded-md border bg-white px-3 py-2 text-sm pr-10',
            'focus:outline-none focus:ring-2 focus:ring-offset-2',
            'disabled:cursor-not-allowed disabled:opacity-50',
            error
              ? 'border-red-500 focus:ring-red-500'
              : 'border-gray-300 focus:ring-blue-500'
          )}
          role="combobox"
          aria-expanded={isOpen}
          aria-autocomplete="list"
          aria-controls="combobox-options"
        />
        <button
          type="button"
          onClick={() => {
            if (!disabled) {
              setIsOpen(!isOpen);
              inputRef.current?.focus();
            }
          }}
          disabled={disabled}
          className="absolute inset-y-0 right-0 flex items-center px-2 text-gray-400"
          tabIndex={-1}
          aria-label="Toggle options"
        >
          <svg
            className={cn('w-5 h-5 transition-transform', isOpen && 'rotate-180')}
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M19 9l-7 7-7-7"
            />
          </svg>
        </button>
      </div>

      {showDropdown && (
        <ul
          ref={listRef}
          id="combobox-options"
          role="listbox"
          className="absolute z-10 w-full mt-1 bg-white border border-gray-300 rounded-md shadow-lg max-h-60 overflow-auto"
        >
          {filteredOptions.map((option, index) => (
            <li
              key={option}
              role="option"
              aria-selected={option === value}
              onClick={() => handleSelect(option)}
              onMouseEnter={() => setHighlightedIndex(index)}
              className={cn(
                'px-3 py-2 text-sm cursor-pointer',
                option === value && 'bg-blue-50 text-blue-700',
                highlightedIndex === index && option !== value && 'bg-gray-100',
                option !== value && highlightedIndex !== index && 'hover:bg-gray-50'
              )}
            >
              {option}
            </li>
          ))}
          {allowCustom && inputValue && !options.includes(inputValue) && (
            <li
              role="option"
              onClick={() => handleSelect(inputValue)}
              onMouseEnter={() => setHighlightedIndex(filteredOptions.length)}
              className={cn(
                'px-3 py-2 text-sm cursor-pointer border-t border-gray-100',
                highlightedIndex === filteredOptions.length ? 'bg-gray-100' : 'hover:bg-gray-50'
              )}
            >
              <span className="text-gray-500">Add:</span>{' '}
              <span className="font-medium">{inputValue}</span>
            </li>
          )}
        </ul>
      )}
    </div>
  );
}
