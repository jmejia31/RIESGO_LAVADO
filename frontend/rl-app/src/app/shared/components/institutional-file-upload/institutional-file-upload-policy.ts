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

export const INSTITUTIONAL_LOGO_FILE_POLICY: InstitutionalFileUploadPolicy = {
  allowedExtensions: ['jpg', 'jpeg', 'png', 'webp'],
  allowedTypesText: 'Logo (JPG, PNG, WebP)',
  maxFileSizeBytes: 5 * 1024 * 1024,
  maxFiles: 1
};

export const INSTITUTIONAL_FAVICON_FILE_POLICY: InstitutionalFileUploadPolicy = {
  allowedExtensions: ['png', 'jpg', 'jpeg', 'webp', 'ico'],
  allowedTypesText: 'Favicon (PNG, JPG, JPEG, WebP, ICO)',
  maxFileSizeBytes: 2 * 1024 * 1024,
  maxFiles: 1
};
