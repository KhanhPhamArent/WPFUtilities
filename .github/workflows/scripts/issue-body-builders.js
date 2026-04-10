/**
 * IssueテンプレートのBodyフォーマット定義
 * GitHub Forms形式テンプレートをAPIで作成する場合は
 * レンダリング後のMarkdown形式で記述する
 */

// ----------------------------------------
// [Implementation] テンプレート対応
// ----------------------------------------
function buildImplementationBody(parentIssueNumber, parentTitle) {
  return [
    `### Categories`,
    ``,
    `Task`,
    ``,
    `### 説明（テキスト）/ Description (Text)`,
    ``,
    `## 実装項目 / Implementation Items`,
    `- [ ] ...の画面作成`,
    `- [ ] ...のロジック作成`,
    `- [ ] ...のNUnitテスト作成`,
    ``,
    `### 確認モデル / Verification Model`,
    ``,
    ``,
    ``,
    `### ブランチ or リリース対象 / Target Branch or Release`,
    ``,
    ``,
    ``,
    `---`,
    ``,
    `> 親Issue: #${parentIssueNumber} ${parentTitle}`,
  ].join('\n');
}

// ----------------------------------------
// [Review] テンプレート対応
// ----------------------------------------
function buildReviewBody(parentIssueNumber, parentTitle) {
  return [
    `### Categories`,
    ``,
    `Task`,
    ``,
    `### 説明（テキスト）/ Description (Text)`,
    ``,
    `## レビュー項目 / Review Items`,
    `- [ ] ロジック確認`,
    `- [ ] 可読性・保守性確認`,
    `- [ ] NUnitテスト確認`,
    ``,
    `### 確認モデル / Verification Model`,
    ``,
    ``,
    ``,
    `### ブランチ or リリース対象 / Target Branch or Release`,
    ``,
    ``,
    ``,
    `---`,
    ``,
    `> 親Issue: #${parentIssueNumber} ${parentTitle}`,
  ].join('\n');
}

// ----------------------------------------
// [Test] テンプレート対応
// ----------------------------------------
function buildTestBody(parentIssueNumber, parentTitle) {
  return [
    `### Categories`,
    ``,
    `Task`,
    ``,
    `### テストに使用するモデル / Model to use for testing`,
    ``,
    ``,
    ``,
    `### テストのドキュメント(UserStoryの場合) / Document for testing (In the UserStory case)`,
    ``,
    ``,
    ``,
    `---`,
    ``,
    `> 親Issue: #${parentIssueNumber} ${parentTitle}`,
  ].join('\n');
}

const { callClaudeAPI } = require('./claude-client.js');

// ----------------------------------------
// Claude API によるBody生成（失敗時は静的テンプレートにフォールバック）
// ----------------------------------------
const CLAUDE_PROMPT_MAP = {
  Implementation: '実装タスク（画面・ロジック・NUnitテスト作成）用',
  Review:         'レビュータスク（ロジック・可読性・NUnitテスト確認）用',
  Test:           'テストタスク（確認モデル・テストドキュメント記載）用',
};

/**
 * 外部送信前にIssue本文を最小化・マスキングする
 * - URL（機密リンク等）を除去
 * - 画像埋め込みを除去
 */
function sanitizeBodyForAI(body) {
  return body
    .replace(/https?:\/\/[^\s)\]]+/g, '[URL省略]')
    .replace(/!\[.*?\]\(.*?\)/g, '[画像省略]');
}

function buildPrompt(subTypeName, parentBody, parentIssueNumber, parentTitle, templateContent) {
  const sanitizedBody = sanitizeBodyForAI(parentBody);

  const templateSection = templateContent
    ? [
        `# SubIssueのIssueテンプレート (GitHub Forms形式)`,
        `以下のYAMLテンプレートのフォームフィールド構造に従って、レンダリング後のMarkdownを出力してください。`,
        '```yaml',
        templateContent,
        '```',
        ``,
      ].join('\n')
    : '';

  return [
    templateSection,
    `# 親IssueのBody`,
    `以下の親IssueのBodyをもとに、${CLAUDE_PROMPT_MAP[subTypeName]}SubIssueのBodyを日本語で生成してください。`,
    `テンプレートのフィールド構造を維持しつつ、親Issueの内容を反映した具体的な記述にしてください。`,
    `末尾に「> 親Issue: #${parentIssueNumber} ${parentTitle}」を必ず含めてください。`,
    ``,
    sanitizedBody,
  ].join('\n');
}

async function buildBodyWithClaude(apiKey, subTypeName, parentBody, fallbackFn, parentIssueNumber, parentTitle, templateContent) {
  if (!apiKey) {
    console.log('ANTHROPIC_API_KEY が未設定のためフォールバックを使用します');
    return fallbackFn(parentIssueNumber, parentTitle);
  }

  try {
    const prompt = buildPrompt(subTypeName, parentBody, parentIssueNumber, parentTitle, templateContent);
    return await callClaudeAPI(apiKey, prompt);
  } catch (e) {
    console.warn(`⚠️ Claude API呼び出し失敗、フォールバックを使用します: ${e.message}`);
    return fallbackFn(parentIssueNumber, parentTitle);
  }
}

module.exports = {
  buildImplementationBody,
  buildReviewBody,
  buildTestBody,
  buildBodyWithClaude,
};