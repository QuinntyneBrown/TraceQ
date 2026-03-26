import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly STORAGE_KEY = 'traceq-theme';
  private readonly darkModeSubject = new BehaviorSubject<boolean>(this.loadTheme());

  isDarkMode$ = this.darkModeSubject.asObservable();

  get isDarkMode(): boolean {
    return this.darkModeSubject.value;
  }

  toggle(): void {
    const newValue = !this.darkModeSubject.value;
    this.darkModeSubject.next(newValue);
    this.saveTheme(newValue);
    this.applyTheme(newValue);
  }

  applyTheme(isDark: boolean): void {
    if (isDark) {
      document.body.classList.add('dark-theme');
    } else {
      document.body.classList.remove('dark-theme');
    }
  }

  initialize(): void {
    this.applyTheme(this.darkModeSubject.value);
  }

  private loadTheme(): boolean {
    try {
      const stored = localStorage.getItem(this.STORAGE_KEY);
      return stored === 'dark';
    } catch {
      return false;
    }
  }

  private saveTheme(isDark: boolean): void {
    try {
      localStorage.setItem(this.STORAGE_KEY, isDark ? 'dark' : 'light');
    } catch {
      // localStorage may not be available in some environments
    }
  }
}
