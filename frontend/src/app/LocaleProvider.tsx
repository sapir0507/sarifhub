import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import createCache from '@emotion/cache';
import { CacheProvider } from '@emotion/react';
import { prefixer } from 'stylis';
import rtlPlugin from 'stylis-plugin-rtl';
import { CssBaseline, ThemeProvider } from '@mui/material';
import { buildTheme } from './theme';
import { dictionaries, type Dictionary, type Language } from './i18n';

/**
 * RTL is handled at the styling layer, not per component:
 * a second Emotion cache runs stylis-plugin-rtl, which flips margins, paddings,
 * borders and positions for every MUI component and every `sx` prop.
 * Content that must stay left-to-right (code, file paths, charts' time axis) opts out with dir="ltr".
 */
const ltrCache = createCache({ key: 'mui', prepend: true });
const rtlCache = createCache({ key: 'muirtl', stylisPlugins: [prefixer, rtlPlugin], prepend: true });

const STORAGE_KEY = 'sarifhub.language';

function readStoredLanguage(): Language {
  try {
    return localStorage.getItem(STORAGE_KEY) === 'he' ? 'he' : 'en';
  } catch {
    return 'en';
  }
}

interface LocaleContextValue {
  language: Language;
  t: Dictionary;
  isRtl: boolean;
  toggleLanguage: () => void;
  formatDate: (iso: string) => string;
  formatDateTime: (iso: string) => string;
}

const LocaleContext = createContext<LocaleContextValue | null>(null);

export function LocaleProvider({ children }: { children: ReactNode }) {
  const [language, setLanguage] = useState<Language>(readStoredLanguage);
  const isRtl = language === 'he';

  useEffect(() => {
    document.documentElement.dir = isRtl ? 'rtl' : 'ltr';
    document.documentElement.lang = language;
    try {
      localStorage.setItem(STORAGE_KEY, language);
    } catch {
      /* storage unavailable: the choice lasts for this session only */
    }
  }, [language, isRtl]);

  const value = useMemo<LocaleContextValue>(() => {
    const locale = language === 'he' ? 'he-IL' : 'en-GB';
    const date = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short', year: 'numeric' });
    const dateTime = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' });
    return {
      language,
      t: dictionaries[language],
      isRtl,
      toggleLanguage: () => setLanguage((l) => (l === 'en' ? 'he' : 'en')),
      formatDate: (iso) => date.format(new Date(iso)),
      formatDateTime: (iso) => dateTime.format(new Date(iso)),
    };
  }, [language, isRtl]);

  const theme = useMemo(() => buildTheme(isRtl ? 'rtl' : 'ltr', language), [isRtl, language]);

  return (
    <LocaleContext.Provider value={value}>
      <CacheProvider value={isRtl ? rtlCache : ltrCache}>
        <ThemeProvider theme={theme}>
          <CssBaseline />
          {children}
        </ThemeProvider>
      </CacheProvider>
    </LocaleContext.Provider>
  );
}

export function useLocale(): LocaleContextValue {
  const ctx = useContext(LocaleContext);
  if (!ctx) throw new Error('useLocale must be used inside LocaleProvider');
  return ctx;
}
