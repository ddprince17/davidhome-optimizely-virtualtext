export type VirtualTextSiteOption = {
  siteId: string | null;
  name: string;
  hosts: string[];
};

export type VirtualTextFileListItem = {
  virtualPath: string;
  siteId: string | null;
  hostName: string | null;
  siteName: string;
  isDefault: boolean;
};

export type VirtualTextFileListResponse = {
  files: VirtualTextFileListItem[];
  hasMore: boolean;
};

export type VirtualTextImportItem = {
  virtualPath: string;
  sourceSiteId: string | null;
  sourceHostName: string | null;
  sourceSiteName: string;
  isUnknownSite: boolean;
  selectedSiteId: string | null;
  selectedHostName: string | null;
};

export type VirtualTextImportListResponse = {
  items: VirtualTextImportItem[];
  hasMore: boolean;
};
