import { Alert, Box, Button, CircularProgress } from '@mui/material';
import type { UseQueryResult } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { useLocale } from '../app/LocaleProvider';
import { ApiError } from '../api/types';

/** One place that decides how loading and failure look, so every page behaves the same. */
export function QueryState<T>({ query, children }: { query: UseQueryResult<T>; children: (data: T) => ReactNode }) {
  const { t } = useLocale();
  if (query.isPending) {
    return (
      <Box sx={{ display: 'grid', placeItems: 'center', py: 10 }} role="status" aria-live="polite">
        <CircularProgress size={28} aria-label={t.common.loading} />
      </Box>
    );
  }
  if (query.isError) {
    const message = query.error instanceof ApiError ? query.error.problem.title : t.common.loadFailed;
    return (
      <Alert
        severity="error"
        action={<Button color="inherit" size="small" onClick={() => void query.refetch()}>{t.common.retry}</Button>}
      >
        {message}
      </Alert>
    );
  }
  return <>{children(query.data)}</>;
}
