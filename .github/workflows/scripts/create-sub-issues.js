const { buildImplementationBody, buildReviewBody, buildTestBody, buildBodyWithClaude } = require('./issue-body-builders.js');
const fs   = require('fs');
const path = require('path');

const SUB_ISSUE_TYPES = [
  {
    name:         'Implementation',
    titlePrefix:  '[Implementation]',
    buildBody:    buildImplementationBody,
    templateFile: '.github/ISSUE_TEMPLATE/implementation.yml',
  },
  {
    name:         'Review',
    titlePrefix:  '[Review]',
    buildBody:    buildReviewBody,
    templateFile: '.github/ISSUE_TEMPLATE/review.yml',
  },
  {
    name:         'Test',
    titlePrefix:  '[Test]',
    buildBody:    buildTestBody,
    templateFile: '.github/ISSUE_TEMPLATE/test.yml',
  },
];

const SUB_ISSUE_CREATED_MARKER = '## 🔖 SubIssueを自動作成しました';

// ----------------------------------------
// SubIssue作成
// ----------------------------------------
async function createSubIssues(github, context, parentIssue, anthropicApiKey) {
  const owner             = context.repo.owner;
  const repo              = context.repo.repo;
  const parentIssueNumber = parentIssue.number;
  const parentTitle       = parentIssue.title;
  const parentBody        = parentIssue.body || '';

  // editedイベントで再実行されても重複作成しないよう
  // 既存コメントの作成済みマーカーを確認する
  const existingComments = await github.rest.issues.listComments({
    owner,
    repo,
    issue_number: parentIssueNumber,
  });

  const alreadyCreated = existingComments.data.some(
    c => c.body && c.body.includes(SUB_ISSUE_CREATED_MARKER)
  );

  if (alreadyCreated) {
    console.log('SubIssueは既に作成済みです。スキップします。');
    return;
  }

  // ラベル「ai-generate」がある場合のみ Claude API へ送信するオプトイン制御
  const hasAiLabel = (parentIssue.labels || []).some(l => l.name === 'ai-generate');
  if (!hasAiLabel) {
    console.log('ラベル "ai-generate" がないため Claude API をスキップし、静的テンプレートを使用します');
    anthropicApiKey = null;
  }

  console.log(`SubIssueを作成します (親Issue: #${parentIssueNumber} "${parentTitle}")`);

  const createdIssueLinks = [];

  for (const subType of SUB_ISSUE_TYPES) {
    const subTitle = `${subType.titlePrefix} ${parentTitle}`;

    let templateContent = null;
    try {
      templateContent = fs.readFileSync(path.resolve(process.cwd(), subType.templateFile), 'utf8');
    } catch (e) {
      console.warn(`⚠️ テンプレートファイル読み込み失敗: ${subType.templateFile}: ${e.message}`);
    }

    const subBody = await buildBodyWithClaude(
      anthropicApiKey,
      subType.name,
      parentBody,
      subType.buildBody,
      parentIssueNumber,
      parentTitle,
      templateContent,
    );

    const created = await github.rest.issues.create({
      owner,
      repo,
      title: subTitle,
      body:  subBody,
    });

    const subIssue = created.data;
    console.log(`✅ SubIssue作成: #${subIssue.number} "${subTitle}"`);
    createdIssueLinks.push(`- #${subIssue.number} ${subTitle}`);

    await linkSubIssue(github, parentIssue.node_id, subIssue.node_id);
  }

  // 親Issueに作成済みマーカー付きコメントを追加
  await github.rest.issues.createComment({
    owner,
    repo,
    issue_number: parentIssueNumber,
    body: [
      ``,
      SUB_ISSUE_CREATED_MARKER,
      ``,
      ...createdIssueLinks,
    ].join('\n'),
  });
}

// ----------------------------------------
// GraphQL: SubIssueとして親Issueにリンク
// ----------------------------------------
async function linkSubIssue(github, parentNodeId, childNodeId) {
  const mutation = `
    mutation($parentIssueId: ID!, $childIssueId: ID!) {
      addSubIssue(input: {
        issueId:    $parentIssueId
        subIssueId: $childIssueId
      }) {
        issue    { id number }
        subIssue { id number }
      }
    }
  `;

  try {
    const result = await github.graphql(mutation, {
      parentIssueId: parentNodeId,
      childIssueId:  childNodeId,
    });
    console.log(
      `🔗 SubIssueリンク完了: 親=#${result.addSubIssue.issue.number} ← 子=#${result.addSubIssue.subIssue.number}`
    );
  } catch (e) {
    console.warn(`⚠️ SubIssueリンクに失敗 (addSubIssue非対応の可能性): ${e.message}`);
  }
}

module.exports = { createSubIssues };