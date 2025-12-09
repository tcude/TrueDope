// Common bullet types for combobox suggestions
// Users can still enter custom types not in this list

export const COMMON_BULLET_TYPES = [
  // Match/Target
  'FMJ',       // Full Metal Jacket
  'HPBT',      // Hollow Point Boat Tail
  'OTM',       // Open Tip Match
  'SMK',       // Sierra MatchKing
  'TMK',       // Tipped MatchKing
  'BTHP',      // Boat Tail Hollow Point
  'ELD-M',     // Hornady Extremely Low Drag - Match
  'A-Tip',     // Hornady A-Tip Match

  // Berger
  'Berger Hybrid',
  'Berger VLD',
  'Berger Elite Hunter',
  'Berger Juggernaut',

  // Hunting/Expanding
  'ELD-X',     // Hornady Extremely Low Drag - Expanding
  'SP',        // Soft Point
  'JSP',       // Jacketed Soft Point
  'JHP',       // Jacketed Hollow Point
  'SST',       // Hornady Super Shock Tip
  'Partition', // Nosler Partition
  'AccuBond',  // Nosler AccuBond
  'E-Tip',     // Nosler E-Tip
  'TTSX',      // Barnes Tipped TSX
  'LRX',       // Barnes Long Range X

  // Other
  'FMJ-BT',    // Full Metal Jacket Boat Tail
  'HP',        // Hollow Point
  'RN',        // Round Nose
  'Scenar',    // Lapua Scenar
  'Scenar-L',  // Lapua Scenar-L
] as const;

export type CommonBulletType = typeof COMMON_BULLET_TYPES[number];

// Drag model options (fixed, not combobox)
export const DRAG_MODELS = ['G1', 'G7'] as const;
export type DragModel = typeof DRAG_MODELS[number];
