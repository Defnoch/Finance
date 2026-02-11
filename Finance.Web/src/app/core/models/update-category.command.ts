export interface UpdateCategoryCommand {
  categoryId: string;
  name: string;
  kind: string;
  colorHex?: string | null;
}
