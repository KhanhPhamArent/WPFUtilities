/**
 * Anthropic Claude API クライアント
 */

/**
 * Claude API を呼び出してテキストを生成する
 * @param {string} apiKey  - Anthropic APIキー
 * @param {string} prompt  - 送信するプロンプト
 * @returns {Promise<string>} - 生成されたテキスト
 */
async function callClaudeAPI(apiKey, prompt) {
  const controller = new AbortController();
  const timeoutMs  = 30_000;
  const timeoutId  = setTimeout(() => controller.abort(), timeoutMs);

  try {
    const response = await fetch('https://api.anthropic.com/v1/messages', {
      method: 'POST',
      headers: {
        'x-api-key':         apiKey,
        'anthropic-version': '2023-06-01',
        'content-type':      'application/json',
      },
      body: JSON.stringify({
        model:      'claude-haiku-4-5',
        max_tokens: 1024,
        messages:   [{ role: 'user', content: prompt }],
      }),
      signal: controller.signal,
    });

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${await response.text()}`);
    }

    const data = await response.json();
    return data.content[0].text;
  } finally {
    clearTimeout(timeoutId);
  }
}

module.exports = { callClaudeAPI };
