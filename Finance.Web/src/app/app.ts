import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AccountYearSelectComponent } from './components/account-year-select.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, AccountYearSelectComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  logout() {
    window.location.href = '/logged-out';
  }

  isLoggedOutPage(): boolean {
    return window.location.pathname === '/logged-out' || window.location.pathname === '/uitloggen';
  }
}
