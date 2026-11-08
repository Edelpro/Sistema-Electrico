import { DOCUMENT } from '@angular/common';
import { inject, Injectable, signal } from '@angular/core';

const THEME_STORAGE_KEY = 'sgse-theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  readonly dark = signal(false);

  constructor() {
    const view = this.document.defaultView;
    let prefersDark = false;

    try {
      const savedTheme = view?.localStorage.getItem(THEME_STORAGE_KEY);
      prefersDark = savedTheme
        ? savedTheme === 'dark'
        : Boolean(view?.matchMedia('(prefers-color-scheme: dark)').matches);
    } catch {
      prefersDark = Boolean(view?.matchMedia('(prefers-color-scheme: dark)').matches);
    }

    this.setDark(prefersDark, false);
  }

  toggle(): void {
    this.setDark(!this.dark());
  }

  setDark(value: boolean, persist = true): void {
    this.dark.set(value);
    this.document.documentElement.dataset['theme'] = value ? 'dark' : 'light';

    if (!persist) {
      return;
    }

    try {
      this.document.defaultView?.localStorage.setItem(THEME_STORAGE_KEY, value ? 'dark' : 'light');
    } catch {
      // Theme persistence is optional when storage is unavailable.
    }
  }
}
