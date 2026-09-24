import { useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { Alert, Box, Button, Stack, TextField, Typography } from '@mui/material';
import TranslateRounded from '@mui/icons-material/TranslateRounded';
import { useAuth } from '../auth/AuthProvider';
import { useLocale } from '../app/LocaleProvider';
import { ApiError } from '../api/types';
import { tokens } from '../app/theme';
import { SeverityRibbon } from '../components/SeverityRibbon';

export function LoginPage() {
  const { t, toggleLanguage } = useLocale();
  const { user, signIn } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState('demo@sarifhub.local');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  if (user) return <Navigate to="/projects" replace />;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await signIn(email, password);
      const from = (location.state as { from?: string } | null)?.from;
      navigate(from ?? '/projects', { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.problem.title : t.common.loadFailed);
    } finally {
      setBusy(false);
    }
  };

  return (
    <Box sx={{ minHeight: '100vh', display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1.1fr 1fr' } }}>
      <Box sx={{ bgcolor: tokens.petrolDeep, color: '#fff', p: { xs: 4, md: 8 }, display: 'flex', flexDirection: 'column', justifyContent: 'space-between', gap: 6 }}>
        <Typography sx={{ fontWeight: 600, fontSize: '1.2rem' }}>SarifHub</Typography>
        <Box sx={{ maxWidth: 520 }}>
          <Typography component="p" sx={{ fontSize: { xs: '1.6rem', md: '2.2rem' }, fontWeight: 600, lineHeight: 1.2, mb: 3 }}>
            {t.login.subtitle}
          </Typography>
          <SeverityRibbon counts={{ critical: 2, high: 14, medium: 37, low: 82 }} />
        </Box>
        <Typography variant="body2" sx={{ opacity: 0.7 }}>SARIF 2.1.0</Typography>
      </Box>

      <Box sx={{ display: 'grid', placeItems: 'center', p: { xs: 3, md: 6 }, position: 'relative' }}>
        <Button size="small" startIcon={<TranslateRounded />} onClick={toggleLanguage} sx={{ position: 'absolute', top: 16, insetInlineEnd: 16 }}>
          {t.common.language}
        </Button>
        <Box component="form" onSubmit={submit} noValidate sx={{ width: '100%', maxWidth: 380 }}>
          <Typography variant="h1" sx={{ mb: 3 }}>{t.login.title}</Typography>
          <Stack spacing={2}>
            <TextField label={t.login.email} type="email" autoComplete="username" value={email} onChange={(e) => setEmail(e.target.value)} required />
            <TextField
              label={t.login.password}
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              autoFocus
            />
            {error && <Alert severity="error">{error}</Alert>}
            <Button type="submit" variant="contained" size="large" loading={busy}>
              {t.login.submit}
            </Button>
            <Typography variant="body2" color="text.secondary">{t.login.demoHint}</Typography>
          </Stack>
        </Box>
      </Box>
    </Box>
  );
}
