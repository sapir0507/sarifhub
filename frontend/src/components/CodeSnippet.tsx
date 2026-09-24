import { Box } from '@mui/material';
import { alpha } from '@mui/material/styles';
import type { CodeSnippet as Snippet } from '../api/types';
import { tokens } from '../app/theme';

export function CodeSnippet({ snippet, severityColor }: { snippet: Snippet; severityColor: string }) {
  return (
    <Box
      dir="ltr"
      component="pre"
      tabIndex={0}
      sx={{
        m: 0,
        py: 1.5,
        overflowX: 'auto',
        bgcolor: '#F8F9FB',
        border: `1px solid ${tokens.line}`,
        borderRadius: 1,
        fontFamily: tokens.fontMono,
        fontSize: '0.82rem',
        lineHeight: 1.7,
      }}
    >
      {snippet.lines.map((text, i) => {
        const lineNo = snippet.startLine + i;
        const hit = lineNo === snippet.highlightLine;
        return (
          <Box
            key={lineNo}
            component="code"
            sx={{
              display: 'flex',
              bgcolor: hit ? alpha(severityColor, 0.1) : 'transparent',
              // Logical properties: the RTL stylis plugin flips physical ones even inside this dir="ltr" island.
              borderInlineStart: `3px solid ${hit ? severityColor : 'transparent'}`,
            }}
          >
            <Box component="span" aria-hidden sx={{ width: 52, flexShrink: 0, textAlign: 'end', paddingInlineEnd: '16px', color: tokens.muted, userSelect: 'none' }}>
              {lineNo}
            </Box>
            <Box component="span" sx={{ whiteSpace: 'pre', paddingInlineEnd: '16px' }}>{text}</Box>
          </Box>
        );
      })}
    </Box>
  );
}
