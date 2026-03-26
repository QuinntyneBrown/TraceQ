import { Component, EventEmitter, Input, Output } from '@angular/core';
import { SearchResultDto } from '../../shared/models/search.model';

@Component({
  selector: 'app-search-results',
  templateUrl: './search-results.component.html',
  styleUrls: ['./search-results.component.scss']
})
export class SearchResultsComponent {
  @Input() results: SearchResultDto[] = [];
  @Output() findSimilar = new EventEmitter<string>();

  getScorePercentage(score: number): number {
    return Math.round(score * 100);
  }

  getScoreClass(score: number): string {
    const pct = this.getScorePercentage(score);
    if (pct >= 80) return 'score-high';
    if (pct >= 60) return 'score-medium';
    return 'score-low';
  }

  getDescriptionSnippet(description: string | null): string {
    if (!description) return 'No description available.';
    return description.length > 200 ? description.substring(0, 200) + '...' : description;
  }

  onFindSimilar(requirementId: string): void {
    this.findSimilar.emit(requirementId);
  }
}
