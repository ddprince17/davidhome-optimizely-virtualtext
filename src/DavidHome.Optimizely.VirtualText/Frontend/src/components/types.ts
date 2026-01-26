export type VirtualTextSiteOption = {
  siteId: string | null;
  name: string;
};

export type VirtualTextFileListItem = {
  virtualPath: string;
  siteId: string | null;
  siteName: string;
  isDefault: boolean;
};

export type VirtualTextFileListResponse = {
  files: VirtualTextFileListItem[];
  hasMore: boolean;
};
