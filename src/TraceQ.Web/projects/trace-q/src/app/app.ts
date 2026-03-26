import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';

interface NavItem {
  path: string;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  readonly navItems: NavItem[] = [
    { path: '/dashboard', label: 'Dashboard', icon: 'dashboard' },
    { path: '/search', label: 'Search', icon: 'search' },
    { path: '/import', label: 'upload', icon: 'upload' },
    { path: '/requirements', label: 'Requirements', icon: 'description' },
  ];

  readonly pageTitle: Record<string, string> = {
    '/dashboard': 'Dashboard',
    '/search': 'Search',
    '/import': 'Import',
    '/requirements': 'Requirements',
  };
}
