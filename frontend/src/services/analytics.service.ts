import api from './api';
import type {
  AnalyticsSummaryDto,
  DopeChartFilterDto,
  DopeChartDataDto,
  VelocityTrendsFilterDto,
  VelocityTrendsDto,
  AmmoComparisonDto,
  LotComparisonDto,
  CostSummaryFilterDto,
  CostSummaryDto,
  ApiResponse,
} from '../types';

export const analyticsService = {
  /**
   * Get analytics summary for dashboard
   */
  getSummary: async (): Promise<AnalyticsSummaryDto> => {
    const response = await api.get<ApiResponse<AnalyticsSummaryDto>>('/analytics/summary');
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.error?.description || 'Failed to fetch analytics summary');
    }
    return response.data.data;
  },

  /**
   * Get DOPE chart data for a rifle with optional filters
   */
  getDopeChart: async (filter: DopeChartFilterDto): Promise<DopeChartDataDto> => {
    const response = await api.get<ApiResponse<DopeChartDataDto>>('/analytics/dope-chart', {
      params: filter,
    });
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.error?.description || 'Failed to fetch DOPE chart data');
    }
    return response.data.data;
  },

  /**
   * Get velocity trends for an ammunition
   */
  getVelocityTrends: async (filter: VelocityTrendsFilterDto): Promise<VelocityTrendsDto> => {
    const response = await api.get<ApiResponse<VelocityTrendsDto>>('/analytics/velocity-trends', {
      params: filter,
    });
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.error?.description || 'Failed to fetch velocity trends');
    }
    return response.data.data;
  },

  /**
   * Compare multiple ammunition types
   */
  compareAmmo: async (ammoIds: number[], rifleId?: number): Promise<AmmoComparisonDto> => {
    // Build query string manually for array params (ASP.NET Core expects repeated params)
    const queryParts: string[] = ammoIds.map((id) => `ammoIds=${id}`);
    if (rifleId) queryParts.push(`rifleId=${rifleId}`);
    const queryString = queryParts.join('&');

    const response = await api.get<ApiResponse<AmmoComparisonDto>>(
      `/analytics/ammo-comparison?${queryString}`
    );
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.error?.description || 'Failed to fetch ammo comparison');
    }
    return response.data.data;
  },

  /**
   * Compare lots for a specific ammunition
   */
  compareLots: async (ammoId: number, rifleId?: number): Promise<LotComparisonDto> => {
    const params: { ammoId: number; rifleId?: number } = { ammoId };
    if (rifleId) params.rifleId = rifleId;

    const response = await api.get<ApiResponse<LotComparisonDto>>('/analytics/lot-comparison', {
      params,
    });
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.error?.description || 'Failed to fetch lot comparison');
    }
    return response.data.data;
  },

  /**
   * Get cost summary with optional filters
   */
  getCostSummary: async (filter?: CostSummaryFilterDto): Promise<CostSummaryDto> => {
    const response = await api.get<ApiResponse<CostSummaryDto>>('/analytics/cost-summary', {
      params: filter,
    });
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.error?.description || 'Failed to fetch cost summary');
    }
    return response.data.data;
  },
};
