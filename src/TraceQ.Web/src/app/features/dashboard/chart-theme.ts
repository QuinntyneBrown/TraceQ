import { Chart, ChartOptions } from 'chart.js';

const DEFENSE_PALETTE: string[] = [
  '#1a237e', // Navy
  '#00897b', // Teal
  '#ff8f00', // Amber
  '#c62828', // Red
  '#4527a0', // Deep Purple
  '#00695c', // Dark Teal
  '#ef6c00', // Orange
  '#283593', // Indigo
  '#2e7d32', // Green
  '#ad1457', // Pink
  '#0277bd', // Light Blue
  '#6a1b9a', // Purple
  '#558b2f', // Light Green
  '#d84315', // Deep Orange
  '#00838f', // Cyan
  '#37474f', // Blue Grey
];

const DEFENSE_PALETTE_TRANSLUCENT: string[] = DEFENSE_PALETTE.map(c => c + 'cc');

export function getChartColors(count: number): string[] {
  const colors: string[] = [];
  for (let i = 0; i < count; i++) {
    colors.push(DEFENSE_PALETTE[i % DEFENSE_PALETTE.length]);
  }
  return colors;
}

export function getChartColorsTranslucent(count: number): string[] {
  const colors: string[] = [];
  for (let i = 0; i < count; i++) {
    colors.push(DEFENSE_PALETTE_TRANSLUCENT[i % DEFENSE_PALETTE_TRANSLUCENT.length]);
  }
  return colors;
}

export function configureChartDefaults(): void {
  Chart.defaults.font.family = 'Roboto, "Helvetica Neue", sans-serif';
  Chart.defaults.font.size = 12;
  Chart.defaults.color = '#616161';
  Chart.defaults.plugins.legend = {
    ...Chart.defaults.plugins.legend,
    labels: {
      ...Chart.defaults.plugins.legend.labels,
      usePointStyle: true,
      padding: 16,
    },
  };
  Chart.defaults.plugins.tooltip = {
    ...Chart.defaults.plugins.tooltip,
    backgroundColor: '#263238',
    titleFont: { size: 13, weight: 'bold', family: 'Roboto, sans-serif', lineHeight: 1.4 },
    bodyFont: { size: 12, weight: 'normal', family: 'Roboto, sans-serif', lineHeight: 1.4 },
    cornerRadius: 4,
    padding: 10,
  };
}

export const DEFAULT_BAR_OPTIONS: ChartOptions<'bar'> = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false },
  },
  scales: {
    x: {
      grid: { display: false },
    },
    y: {
      beginAtZero: true,
      ticks: { precision: 0 },
    },
  },
};

export const DEFAULT_HORIZONTAL_BAR_OPTIONS: ChartOptions<'bar'> = {
  responsive: true,
  maintainAspectRatio: false,
  indexAxis: 'y',
  plugins: {
    legend: { display: false },
  },
  scales: {
    x: {
      beginAtZero: true,
      ticks: { precision: 0 },
    },
    y: {
      grid: { display: false },
    },
  },
};

export const DEFAULT_PIE_OPTIONS: ChartOptions<'pie'> = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'right',
    },
  },
};

export const DEFAULT_DOUGHNUT_OPTIONS: ChartOptions<'doughnut'> = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'right',
    },
  },
};
