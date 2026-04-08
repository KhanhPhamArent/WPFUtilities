const { createSubIssues } = require('./create-sub-issues.js');

const PROJECT_ID = 'PVT_kwDOBM6-h84BDNP4';

module.exports = async function main({ github, context }) {
  const issue     = context.payload.issue;
  const issueBody = issue.body || '';

  // Issueテンプレートから「Categories」の値を抽出
  const taskClassificationMatch = issueBody.match(/### Categories[ \t]*\r?\n\r?\n[ \t]*([^\r\n]+)/);
  const taskClassification = taskClassificationMatch
    ? taskClassificationMatch[1].trim()
    : null;

  console.log(`Issue #${issue.number}: Categories = ${taskClassification}`);

  if (!taskClassification) {
    console.log('Categoriesが見つかりません');
    return;
  }

  // UserStory・Bugの場合、新規作成時のみSubIssueを作成する
  const isOpened = context.payload.action === 'opened';
  if (isOpened && (taskClassification === 'UserStory' || taskClassification === 'Bug')) {
    await createSubIssues(github, context, issue, process.env.ANTHROPIC_API_KEY);
  }

  // ----------------------------------------
  // Project同期
  // ----------------------------------------

  // Projectに該当IssueのアイテムIDを取得または追加
  const queryResult = await github.graphql(`
    query($owner: String!, $repo: String!, $issueNumber: Int!) {
      repository(owner: $owner, name: $repo) {
        issue(number: $issueNumber) {
          projectItems(first: 10) {
            nodes {
              id
              project { id }
            }
          }
        }
      }
    }
  `, {
    owner:       context.repo.owner,
    repo:        context.repo.repo,
    issueNumber: issue.number,
  });

  let projectItemId  = null;
  const projectItems = queryResult.repository.issue.projectItems.nodes;
  const existingItem = projectItems.find(item => item.project.id === PROJECT_ID);

  if (existingItem) {
    projectItemId = existingItem.id;
    console.log('既存のProjectアイテムを使用:', projectItemId);
  } else {
    const addResult = await github.graphql(`
      mutation($projectId: ID!, $contentId: ID!) {
        addProjectV2ItemById(input: {
          projectId: $projectId
          contentId: $contentId
        }) {
          item { id }
        }
      }
    `, {
      projectId: PROJECT_ID,
      contentId: issue.node_id,
    });

    projectItemId = addResult.addProjectV2ItemById.item.id;
    console.log('Projectに新規追加:', projectItemId);
  }

  // Categoriesの選択肢IDを取得
  const fieldResult = await github.graphql(`
    query($projectId: ID!) {
      node(id: $projectId) {
        ... on ProjectV2 {
          field(name: "Categories") {
            ... on ProjectV2SingleSelectField {
              id
              options { id name }
            }
          }
        }
      }
    }
  `, { projectId: PROJECT_ID });

  const field          = fieldResult.node.field;
  const selectedOption = field.options.find(opt => opt.name === taskClassification);

  if (!selectedOption) {
    console.log(`選択肢 "${taskClassification}" が見つかりません`);
    return;
  }

  // Projectフィールドを更新
  await github.graphql(`
    mutation($projectId: ID!, $itemId: ID!, $fieldId: ID!, $value: ProjectV2FieldValue!) {
      updateProjectV2ItemFieldValue(input: {
        projectId: $projectId
        itemId:    $itemId
        fieldId:   $fieldId
        value:     $value
      }) {
        projectV2Item { id }
      }
    }
  `, {
    projectId: PROJECT_ID,
    itemId:    projectItemId,
    fieldId:   field.id,
    value:     { singleSelectOptionId: selectedOption.id },
  });

  console.log('✅ Categoriesを更新しました:', taskClassification);
};