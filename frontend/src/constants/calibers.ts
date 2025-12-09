// Common calibers for combobox suggestions
// Users can still enter custom calibers not in this list

export const COMMON_CALIBERS = [
  // Rimfire
  '.22 LR',
  '.17 HMR',
  '.22 WMR',

  // Pistol
  '9mm Luger',
  '.45 ACP',
  '.40 S&W',
  '10mm Auto',
  '.357 Magnum',
  '.44 Magnum',
  '.380 ACP',

  // Rifle - Common
  '.223 Remington',
  '5.56 NATO',
  '.308 Winchester',
  '7.62 NATO',
  '.30-06 Springfield',
  '.300 Blackout',

  // Rifle - Precision/Long Range
  '6.5 Creedmoor',
  '6.5 PRC',
  '.260 Remington',
  '6mm Creedmoor',
  '.243 Winchester',
  '.338 Lapua Magnum',
  '.300 Winchester Magnum',
  '.300 PRC',
  '.300 Norma Magnum',
  '6.5x47 Lapua',

  // Rifle - Other Popular
  '.270 Winchester',
  '7mm Remington Magnum',
  '7mm PRC',
  '.350 Legend',
  '.450 Bushmaster',
  '.224 Valkyrie',
  '6mm ARC',
  '6.8 Western',
  '.204 Ruger',
  '.22-250 Remington',

  // Shotgun
  '12 Gauge',
  '20 Gauge',
  '.410 Bore',
] as const;

export type CommonCaliber = typeof COMMON_CALIBERS[number];
