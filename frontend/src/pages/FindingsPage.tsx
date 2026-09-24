import { useParams } from 'react-router-dom';
import { useLocale } from '../app/LocaleProvider';
import { PageHeader } from '../components/AppShell';
import { FindingsExplorer } from '../components/FindingsExplorer';

export function FindingsPage() {
  const { projectId = '' } = useParams();
  const { t } = useLocale();
  return (
    <>
      <PageHeader title={t.findings.title} />
      <FindingsExplorer projectId={projectId} />
    </>
  );
}
