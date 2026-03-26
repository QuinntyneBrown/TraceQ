import { Component, OnInit, ViewChild } from '@angular/core';
import { MatSidenav } from '@angular/material/sidenav';
import { BreakpointObserver } from '@angular/cdk/layout';
import { ThemeService } from './layout/theme.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  @ViewChild('sidenav') sidenav!: MatSidenav;

  sidenavOpened = true;
  isMobile = false;

  constructor(
    private themeService: ThemeService,
    private breakpointObserver: BreakpointObserver
  ) {}

  ngOnInit(): void {
    this.themeService.initialize();

    this.breakpointObserver.observe(['(max-width: 768px)'])
      .subscribe(result => {
        this.isMobile = result.matches;
        this.sidenavOpened = !result.matches;
      });
  }

  toggleSidenav(): void {
    if (this.isMobile) {
      this.sidenav.toggle();
    } else {
      this.sidenavOpened = !this.sidenavOpened;
    }
  }
}
