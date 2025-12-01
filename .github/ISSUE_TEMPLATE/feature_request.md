---
name: Feature request
about: Suggest an idea for this project
title: ''
labels: ''
assignees: ''

---

## 📝 Description
<!-- Provide a clear and concise description of what this PR does -->

## 🔗 Related Issues
<!-- Link to the issue(s) this PR addresses -->
Closes #(issue number)
Fixes #(issue number)
Relates to #(issue number)

## 🎯 Type of Change
<!-- Check all that apply -->
- [ ] 🐛 Bug fix (non-breaking change which fixes an issue)
- [ ] ✨ New feature (non-breaking change which adds functionality)
- [ ] 💥 Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] 📝 Documentation update
- [ ] 🎨 UI/UX improvement
- [ ] ♻️ Code refactoring (no functional changes)
- [ ] 🧪 Test addition or update
- [ ] 🔒 Security enhancement
- [ ] ⚡ Performance improvement

## 📋 Changes Made
<!-- List the main changes in this PR -->
- Change 1
- Change 2
- Change 3

## 🧪 Testing
<!-- Describe the tests you ran to verify your changes -->

**Test Configuration:**
- Windows Version: [e.g. Windows 11 23H2]
- .NET Version: [e.g. .NET 10.0]
- Visual Studio Version: [e.g. VS 2022 17.12]

**Tests Performed:**
- [ ] All existing unit tests pass
- [ ] New unit tests added and passing
- [ ] Manual testing completed
- [ ] Tested on clean installation
- [ ] Tested with existing configuration

**Test Scenarios:**
<!-- Describe specific scenarios you tested -->
1. Scenario 1
2. Scenario 2
3. Scenario 3

## 📡 SmartThings Compatibility
<!-- Critical: Ensure SmartThings Edge Driver integration still works -->

- [ ] **No API changes** - This PR doesn't affect HTTP endpoints
- [ ] **API changes are backward compatible** - Existing endpoints work as before
- [ ] **Breaking API changes** - Requires SmartThings Edge Driver update (⚠️ requires coordination)

**If API was modified:**
- [ ] All existing endpoint URLs remain unchanged
- [ ] HTTP methods remain unchanged (GET stays GET, etc.)
- [ ] Authentication mechanisms still work (Bearer token + URL-based)
- [ ] Response status codes unchanged for existing scenarios
- [ ] Tested all endpoints with curl/PowerShell
- [ ] Documented new endpoints in README

**Endpoints Affected:**
<!-- List any endpoints that were added or modified -->
- `/endpoint-name` - [Added/Modified/Removed]

**SmartThings Testing:**
```bash
# Commands used to verify compatibility
curl -H "Authorization: Bearer test-secret" http://localhost:5005/shutdown
curl http://localhost:5005/test-secret/restart
# Add other test commands here
```

## 🖼️ Screenshots/Videos
<!-- If applicable, add screenshots or videos demonstrating the changes -->
<!-- Especially important for UI changes! -->

**Before:**
<!-- Screenshots of the old behavior/UI -->

**After:**
<!-- Screenshots of the new behavior/UI -->

## 📚 Documentation
<!-- Have you updated relevant documentation? -->
- [ ] README.md updated (if needed)
- [ ] CONTRIBUTING.md updated (if needed)
- [ ] Code comments added/updated
- [ ] XML documentation comments added for public APIs
- [ ] API Reference section updated (if endpoints changed)

## ⚠️ Breaking Changes
<!-- List any breaking changes and migration steps -->
<!-- If none, you can delete this section -->

**Breaking Changes:**
- Breaking change 1
- Breaking change 2

**Migration Guide:**
<!-- How should users/developers adapt to these changes? -->
1. Step 1
2. Step 2

## 🔒 Security Considerations
<!-- Does this PR have any security implications? -->
- [ ] No security implications
- [ ] Security-related change (describe below)

**Security Notes:**
<!-- If applicable, describe security considerations -->

## 📊 Performance Impact
<!-- Does this PR affect performance? -->
- [ ] No performance impact
- [ ] Performance improved
- [ ] Potential performance impact (describe below)

**Performance Notes:**
<!-- If applicable, describe performance considerations -->

## ✅ Checklist
<!-- Make sure you've completed all items before submitting -->

**Code Quality:**
- [ ] My code follows the project's style guidelines
- [ ] I have performed a self-review of my own code
- [ ] I have commented my code, particularly in hard-to-understand areas
- [ ] My changes generate no new compiler warnings
- [ ] I have added error handling where appropriate

**Testing:**
- [ ] I have added tests that prove my fix is effective or that my feature works
- [ ] New and existing unit tests pass locally with my changes
- [ ] I have tested this on a clean Windows installation
- [ ] SmartThings compatibility verified (if API changed)

**Documentation:**
- [ ] I have updated the documentation accordingly
- [ ] I have updated the README if needed
- [ ] I have added XML comments for new public APIs

**Git Hygiene:**
- [ ] My commits are clean and have descriptive messages
- [ ] I have rebased on the latest `main` branch
- [ ] I have resolved any merge conflicts

## 🤔 Additional Context
<!-- Any other information that reviewers should know -->

## 📸 Demo
<!-- Optional: Link to a demo video or GIF showing the feature in action -->

---

## 👀 Reviewer Notes
<!-- Optional: Anything specific you'd like reviewers to focus on? -->

## 🎉 Post-Merge Tasks
<!-- Optional: Any tasks that need to be done after merging? -->
- [ ] Update SmartThings Edge Driver (if needed)
- [ ] Create release notes
- [ ] Update documentation site
- [ ] Notify users of breaking changes

---

**By submitting this PR, I confirm that:**
- [ ] My contribution is made under the same license as the project
- [ ] I have the right to submit this code
- [ ] I understand that this may be publicly visible and reviewed by others
