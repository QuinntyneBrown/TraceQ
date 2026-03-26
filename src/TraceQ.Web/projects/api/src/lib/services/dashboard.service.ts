import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api-base-url.token';
import { DashboardLayout } from '../models/dashboard.model';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getLayouts(): Observable<DashboardLayout[]> {
    return this.http.get<DashboardLayout[]>(`${this.baseUrl}/api/dashboard/layouts`);
  }

  saveLayout(layout: DashboardLayout): Observable<DashboardLayout> {
    return this.http.post<DashboardLayout>(`${this.baseUrl}/api/dashboard/layouts`, layout);
  }

  deleteLayout(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/api/dashboard/layouts/${id}`);
  }
}
