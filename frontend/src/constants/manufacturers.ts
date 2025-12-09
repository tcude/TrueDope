// Common ammunition manufacturers for combobox suggestions
// Users can still enter custom manufacturers not in this list (e.g., for handloads)

export const COMMON_MANUFACTURERS = [
  'Federal',
  'Hornady',
  'Nosler',
  'Barnes',
  'Sierra',
  'Lapua',
  'Berger',
  'Norma',
  'Winchester',
  'Remington',
  'Speer',
  'PMC',
  'Fiocchi',
  'Sellier & Bellot',
  'Prvi Partizan',
  'Black Hills',
  'Sig Sauer',
  'Aguila',
  'CCI',
  'Magtech',
  'American Eagle',
  'HSM',
  'Weatherby',
  'Cutting Edge',
  'Handload', // For reloaders
] as const;

export type CommonManufacturer = typeof COMMON_MANUFACTURERS[number];
