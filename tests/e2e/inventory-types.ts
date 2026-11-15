export type InventoryPlaywright =
  | { strategy: 'goto'; path: string }
  | { strategy: 'role'; role: 'button' | 'link' | 'tab'; name: string }
  | { strategy: 'link'; name: string }
  | { strategy: 'tab'; name: string }
  | { strategy: 'help' }
  | { strategy: 'assistant-chat' }
  | { strategy: 'missing-control' };

export interface InventoryItem {
  id: string;
  page: string;
  route: string;
  label: string;
  type: string;
  fileLine: string;
  intendedBehavior: string;
  dependencies: string[];
  permissions: string[];
  expectedResult: string;
  testId: string;
  smokeSafe: boolean;
  writesData: boolean;
  knownFailure: string | null;
  playwright: InventoryPlaywright;
}

export interface FunctionsInventory {
  meta: Record<string, number | string>;
  items: InventoryItem[];
}