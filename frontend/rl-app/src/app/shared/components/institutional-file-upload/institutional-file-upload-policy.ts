export interface InstitutionalFileUploadPolicy {
  readonly allowedExtensions: readonly string[];
  readonly allowedTypesText: string;
  readonly maxFileSizeBytes?: number;
  readonly maxFiles?: number;
}

export const INSTITUTIONAL_EVIDENCE_FILE_POLICY: InstitutionalFileUploadPolicy = {
  allowedExtensions: ['pdf', 'png', 'jpg', 'jpeg', 'doc', 'docx', 'xls', 'xlsx'],
  allowedTypesText: 'PDF, imágenes, Word, Excel',
  maxFileSizeBytes: 10 * 1024 * 1024,
};

export const LIST_IMPORT_FILE_POLICY: InstitutionalFileUploadPolicy = {
  allowedExtensions: ['csv', 'xlsx', 'xml'],
  allowedTypesText: 'CSV, Excel, XML',
};

export const LOGIN_SLIDE_IMAGE_FILE_POLICY: InstitutionalFileUploadPolicy = {
  allowedExtensions: ['jpg', 'jpeg', 'png', 'gif', 'webp'],
  allowedTypesText: 'Imágenes (JPG, PNG, GIF, WebP)',
};
