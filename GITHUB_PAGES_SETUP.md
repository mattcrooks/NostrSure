# GitHub Pages Setup Instructions

The coverage reporting and badge system is working correctly, but GitHub Pages needs to be enabled in the repository settings to make the links accessible.

## Issue
- ❌ https://mattcrooks.github.io/NostrSure/ returns 404 
- ❌ Coverage badge shows broken link: https://mattcrooks.github.io/NostrSure/coverage-badge.svg

## Root Cause
GitHub Pages is not enabled for this repository. The coverage files are successfully deployed to the `gh-pages` branch by the CI workflow, but GitHub isn't serving them.

## Solution (Repository Owner Only)
1. Go to repository **Settings** → **Pages** (left sidebar)
2. Under **Source**, select: **Deploy from a branch**
3. Under **Branch**, select: **gh-pages** / **(root)**
4. Click **Save**

GitHub will then start serving the coverage reports and badge from your `gh-pages` branch.

## Expected Result After Setup
- ✅ https://mattcrooks.github.io/NostrSure/ - Interactive coverage report
- ✅ https://mattcrooks.github.io/NostrSure/coverage-badge.svg - Coverage badge (currently 63.5%)
- ✅ README badges will display correctly

## Current Status
- **CI Workflow**: ✅ Working (all 101 tests pass)
- **Coverage Generation**: ✅ Working (63.5% line coverage)
- **Badge Generation**: ✅ Working (SVG generated with proper colors)
- **GitHub Pages Deployment**: ✅ Working (files in gh-pages branch)
- **GitHub Pages Serving**: ❌ Needs repository settings change

The infrastructure is complete - only the final GitHub Pages enablement step remains.