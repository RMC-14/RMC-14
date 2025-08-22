var ic=Object.defineProperty;var i=(Il,li)=>ic(Il,"name",{value:li,configurable:!0});(()=>{var Il={2410:(M,R,J)=>{"use strict";J.d(R,{A:i(()=>y,"A")});var oe=J(76314),le=J.n(oe),I=le()(function(v){return v[1]});I.push([M.id,`/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

body a {
	text-decoration: var(--text-link-decoration);
}

h3 {
	display: unset;
	font-size: unset;
	margin-block-start: unset;
	margin-block-end: unset;
	margin-inline-start: unset;
	margin-inline-end: unset;
	font-weight: unset;
}

body a:hover {
	text-decoration: underline;
}

button,
input[type='submit'] {
	color: var(--vscode-button-foreground);
	font-family: var(--vscode-font-family);
	border-radius: 2px;
	border: 1px solid transparent;
	padding: 4px 12px;
	font-size: 13px;
	line-height: 18px;
	white-space: nowrap;
	user-select: none;
}

button:not(.icon-button):not(.danger):not(.secondary),
input[type='submit'] {
	background-color: var(--vscode-button-background);
}

input.select-left {
	border-radius: 2px 0 0 2px;
}

button.select-right {
	border-radius: 0 2px 2px 0;
}

button:focus,
input[type='submit']:focus {
	outline-color: var(--vscode-focusBorder);
	outline-style: solid;
	outline-width: 1px;
	outline-offset: 2px;
}

button:hover:enabled,
button:focus:enabled,
input[type='submit']:focus:enabled,
input[type='submit']:hover:enabled {
	background-color: var(--vscode-button-hoverBackground);
	cursor: pointer;
}

button.secondary {
	background-color: var(--vscode-button-secondaryBackground);
	color: var(--vscode-button-secondaryForeground);
}

button.secondary:hover:enabled,
button.secondary:focus:enabled,
input[type='submit'].secondary:focus:enabled,
input[type='submit'].secondary:hover:enabled {
	background-color: var(--vscode-button-secondaryHoverBackground);
}

textarea,
input[type='text'] {
	display: block;
	box-sizing: border-box;
	padding: 8px;
	width: 100%;
	resize: vertical;
	font-size: 13px;
	border: 1px solid var(--vscode-dropdown-border);
	background-color: var(--vscode-input-background);
	color: var(--vscode-input-foreground);
	font-family: var(--vscode-font-family);
	border-radius: 2px;
}

textarea::placeholder,
input[type='text']::placeholder {
	color: var(--vscode-input-placeholderForeground);
}

select {
	display: block;
	box-sizing: border-box;
	padding: 4px 8px;
	border-radius: 2px;
	font-size: 13px;
	border: 1px solid var(--vscode-dropdown-border);
	background-color: var(--vscode-dropdown-background);
	color: var(--vscode-dropdown-foreground);
}

textarea:focus,
input[type='text']:focus,
input[type='checkbox']:focus,
select:focus {
	outline: 1px solid var(--vscode-focusBorder);
}

input[type='checkbox'] {
	outline-offset: 1px;
}

.vscode-high-contrast input[type='checkbox'] {
	outline: 1px solid var(--vscode-contrastBorder);
}

.vscode-high-contrast input[type='checkbox']:focus {
	outline: 1px solid var(--vscode-contrastActiveBorder);
}

svg path {
	fill: var(--vscode-foreground);
}

body button:disabled,
input[type='submit']:disabled {
	opacity: 0.4;
}

body .hidden {
	display: none !important;
}

body img.avatar,
body span.avatar-icon svg {
	width: 20px;
	height: 20px;
	border-radius: 50%;
}

body img.avatar {
	vertical-align: middle;
}

.avatar-link {
	flex-shrink: 0;
}

.icon-button {
	display: flex;
	padding: 2px;
	background: transparent;
	border-radius: 4px;
	line-height: 0;
}

.icon-button:hover,
.section .icon-button:hover,
.section .icon-button:focus {
	background-color: var(--vscode-toolbar-hoverBackground);
}

.icon-button:focus,
.section .icon-button:focus {
	outline: 1px solid var(--vscode-focusBorder);
	outline-offset: 1px;
}

.label .icon-button:hover,
.label .icon-button:focus {
	background-color: transparent;
}

.section-item {
	display: flex;
	align-items: center;
	justify-content: space-between;
}

.section-item .avatar-link {
	margin-right: 8px;
}

.section-item .avatar-container {
	flex-shrink: 0;
}

.section-item .login {
	width: 129px;
	flex-shrink: 0;
}

.section-item img.avatar {
	width: 20px;
	height: 20px;
}

.section-icon {
	display: flex;
	align-items: center;
	justify-content: center;
	padding: 3px;
}

.section-icon.changes svg path {
	fill: var(--vscode-list-errorForeground);
}

.section-icon.commented svg path,
.section-icon.requested svg path {
	fill: var(--vscode-list-warningForeground);
}

.section-icon.approved svg path {
	fill: var(--vscode-issues-open);
}

.reviewer-icons {
	display: flex;
	gap: 4px;
}

.push-right {
	margin-left: auto;
}

.avatar-with-author {
	display: flex;
	align-items: center;
}

.author-link {
	font-weight: 600;
	color: var(--vscode-editor-foreground);
}

.status-item button {
	margin-left: auto;
	margin-right: 0;
}

.automerge-section {
	display: flex;
}

.automerge-section,
.status-section {
	flex-wrap: wrap;
}

#status-checks .automerge-section {
	align-items: center;
	padding: 16px;
	background: var(--vscode-editorHoverWidget-background);
	border-bottom-left-radius: 3px;
	border-bottom-right-radius: 3px;
}

.automerge-section .merge-select-container {
	margin-left: 8px;
}

.automerge-checkbox-wrapper,
.automerge-checkbox-label {
	display: flex;
	align-items: center;
	margin-right: 4px;
}

.automerge-checkbox-label {
	min-width: 80px;
}

.merge-queue-title .merge-queue-pending {
	color: var(--vscode-list-warningForeground);
}

.merge-queue-title .merge-queue-blocked {
	color: var(--vscode-list-errorForeground);
}

.merge-queue-title {
	font-weight: bold;
	font-size: larger;
}

/** Theming */

.vscode-high-contrast button:not(.secondary):not(.icon-button) {
	background: var(--vscode-button-background);
}


.vscode-high-contrast input {
	outline: none;
	background: var(--vscode-input-background);
	border: 1px solid var(--vscode-contrastBorder);
}

.vscode-high-contrast button:focus {
	border: 1px solid var(--vscode-contrastActiveBorder);
}

.vscode-high-contrast button:hover {
	border: 1px dotted var(--vscode-contrastActiveBorder);
}

::-webkit-scrollbar-corner {
	display: none;
}

.labels-list {
	display: flex;
	flex-wrap: wrap;
	gap: 8px;
}

.label {
	display: flex;
	justify-content: normal;
	padding: 0 8px;
	border-radius: 20px;
	border-style: solid;
	border-width: 1px;
	background: var(--vscode-badge-background);
	color: var(--vscode-badge-foreground);
	font-size: 11px;
	line-height: 18px;
	font-weight: 600;
}

/* split button */

.primary-split-button {
	display: flex;
	flex-grow: 1;
	min-width: 0;
	max-width: 260px;
}

button.split-left {
	border-radius: 2px 0 0 2px;
	flex-grow: 1;
	overflow: hidden;
	white-space: nowrap;
	text-overflow: ellipsis;
}

.split {
	width: 1px;
	height: 100%;
	background-color: var(--vscode-button-background);
	opacity: 0.5;
}

button.split-right {
	border-radius: 0 2px 2px 0;
	cursor: pointer;
	width: 24px;
	height: 28px;
	position: relative;
}

button.split-right:disabled {
	cursor: default;
}

button.split-right .icon {
	pointer-events: none;
	position: absolute;
	top: 6px;
	right: 4px;
}

button.split-right .icon svg path {
	fill: unset;
}
button.input-box {
	display: block;
	height: 24px;
	margin-top: -4px;
	padding-top: 2px;
	padding-left: 8px;
	text-align: left;
	overflow: hidden;
	white-space: nowrap;
	text-overflow: ellipsis;
	color: var(--vscode-input-foreground) !important;
	background-color: var(--vscode-input-background) !important;
}

button.input-box:active,
button.input-box:focus {
	color: var(--vscode-inputOption-activeForeground) !important;
	background-color: var(--vscode-inputOption-activeBackground) !important;
}

button.input-box:hover:not(:disabled) {
	background-color: var(--vscode-inputOption-hoverBackground) !important;
}

button.input-box:focus {
	border-color: var(--vscode-focusBorder) !important;
}

.dropdown-container {
	display: flex;
	flex-grow: 1;
	min-width: 0;
	margin: 0;
	width: 100%;
}

button.inlined-dropdown {
	width: 100%;
	max-width: 150px;
	margin-right: 5px;
	display: inline-block;
	text-align: center;
}`,""]);const y=I},3554:(M,R,J)=>{"use strict";J.d(R,{A:i(()=>y,"A")});var oe=J(76314),le=J.n(oe),I=le()(function(v){return v[1]});I.push([M.id,`/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

#app {
	display: grid;
	grid-template-columns: 1fr minmax(200px, 300px);
	column-gap: 32px;
}

#title {
	grid-column-start: 1;
	grid-column-end: 3;
	grid-row: 1;
}

#main {
	grid-column: 1;
	grid-row: 2;
	display: flex;
	flex-direction: column;
	gap: 16px;
}

#sidebar {
	display: flex;
	flex-direction: column;
	gap: 16px;
	grid-column: 2;
	grid-row: 2;
}

#project a {
	cursor: pointer;
}

a:focus,
input:focus,
select:focus,
textarea:focus,
.title-text:focus {
	outline: 1px solid var(--vscode-focusBorder);
}

.title-text {
	margin-right: 5px;
}

.title {
	display: flex;
	align-items: flex-start;
	margin: 20px 0;
	padding-bottom: 24px;
	border-bottom: 1px solid var(--vscode-list-inactiveSelectionBackground);
}

.title .pr-number {
	margin-left: 5px;
}

.loading-indicator {
	position: fixed;
	top: 50%;
	left: 50%;
	transform: translate(-50%, -50%);
}

.comment-body li div {
	display: inline;
}

.comment-body li div.Box,
.comment-body li div.Box div {
	display: block;
}

.comment-body code,
.comment-body a,
span.lineContent {
	overflow-wrap: anywhere;
}

.comment-reactions {
	display: flex;
	flex-direction: row;
}

.comment-reactions div {
	font-size: 1.1em;
	cursor: pointer;
	user-select: none;
}

.comment-reactions .reaction-label {
	border-radius: 5px;
	border: 1px solid var(--vscode-panel-border);
	width: 14px;
}

#title:empty {
	border: none;
}

h2 {
	margin: 0;
}

body hr {
	display: block;
	height: 1px;
	border: 0;
	border-top: 1px solid #555;
	margin: 0 !important;
	padding: 0;
}

body .comment-container .avatar-container {
	margin-right: 12px;
}

body .comment-container .avatar-container a {
	display: flex;
}

body .comment-container .avatar-container img.avatar,
body .comment-container .avatar-container .avatar-icon svg {
	margin-right: 0;
}

.vscode-light .avatar-icon {
	filter: invert(100%);
}

body a.avatar-link:focus {
	outline-offset: 2px;
}

body .comment-container.comment,
body .comment-container.review {
	background-color: var(--vscode-editor-background);
}

.review-comment-container {
	width: 100%;
	max-width: 1000px;
	display: flex;
	flex-direction: column;
	position: relative;
}

body #main>.comment-container>.review-comment-container>.review-comment-header:not(:nth-last-child(2)) {
	border-bottom: 1px solid var(--vscode-editorHoverWidget-border);
}

body .comment-container .review-comment-header {
	position: relative;
	display: flex;
	width: 100%;
	box-sizing: border-box;
	padding: 8px 16px;
	color: var(--vscode-foreground);
	align-items: center;
	background: var(--vscode-editorWidget-background);
	border-top-left-radius: 3px;
	border-top-right-radius: 3px;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.review-comment-header.no-details {
	border-bottom-left-radius: 3px;
	border-bottom-right-radius: 3px;
}

.description-header {
	float: right;
	height: 32px;
}

.review-comment-header .comment-actions {
	margin-left: auto;
}

.review-comment-header .pending {
	color: inherit;
	font-style: italic;
}

.comment-actions button {
	background-color: transparent;
	padding: 0;
	line-height: normal;
	font-size: 11px;
}

.comment-actions button svg {
	margin-right: 0;
	height: 14px;
}

.comment-actions .icon-button {
	padding-left: 2px;
	padding-top: 2px;
}

.status-scroll {
	max-height: 220px;
	overflow-y: auto;
}

.status-check {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 12px 16px;
	border-bottom: 1px solid var(--vscode-editorHoverWidget-border);
}

.status-check-details {
	display: flex;
	align-items: center;
	gap: 8px;
}

#merge-on-github {
	margin-top: 10px;
}

.status-item {
	padding: 12px 16px;
	border-bottom: 1px solid var(--vscode-editorHoverWidget-border);
}

.status-item:first-of-type {
	background: var(--vscode-editorWidget-background);
	border-top-left-radius: 3px;
	border-top-right-radius: 3px;
}

.status-item,
.form-actions,
.ready-for-review-text-wrapper {
	display: flex;
	gap: 8px;
	align-items: center;
}

.status-item .button-container {
	margin-left: auto;
	margin-right: 0;
}

.commit-association {
	display: flex;
	font-style: italic;
	flex-direction: row-reverse;
	padding-top: 7px;
}

.commit-association span {
	flex-direction: row;
}

.email {
	font-weight: bold;
}

button.input-box {
	float: right;
}

.status-item-detail-text {
	display: flex;
	gap: 8px;
}

.status-check-detail-text {
	margin-right: 8px;
}

.status-section p {
	margin: 0;
}

.status-section .check svg path {
	fill: var(--vscode-issues-open);
}

.status-section .close svg path {
	fill: var(--vscode-errorForeground);
}

.status-section .pending svg path,
.status-section .skip svg path {
	fill: var(--vscode-list-warningForeground);
}

.merge-queue-container,
.ready-for-review-container {
	padding: 16px;
	background-color: var(--vscode-editorWidget-background);
	border-bottom-left-radius: 3px;
	border-bottom-right-radius: 3px;
	display: flex;
	justify-content: space-between;
	align-items: center;
}

.ready-for-review-icon {
	width: 16px;
	height: 16px;
}

.ready-for-review-heading {
	font-weight: 600;
}

.ready-for-review-meta {
	font-size: 0.9;
}

#status-checks {
	border: 1px solid var(--vscode-editorHoverWidget-border);
	border-radius: 4px;
}

#status-checks .label {
	display: inline-flex;
	margin-right: 16px;
}

#status-checks a {
	cursor: pointer;
}

#status-checks summary {
	display: flex;
	align-items: center;
}

#status-checks-display-button {
	margin-left: auto;
}

#status-checks .avatar-link svg {
	width: 24px;
	margin-right: 0px;
	vertical-align: middle;
}

.status-check .avatar-link .avatar-icon {
	margin-right: 0px;
}

#status-checks .merge-select-container {
	display: flex;
	align-items: center;
	background-color: var(--vscode-editorWidget-background);
	border-bottom-left-radius: 3px;
	border-bottom-right-radius: 3px;
}

#status-checks .merge-select-container>* {
	margin-right: 5px;
}

#status-checks .merge-select-container>select {
	margin-left: 5px;
}

#status-checks .branch-status-container {
	display: inline-block;
}

#status-checks .branch-status-message {
	display: inline-block;
	line-height: 100%;
	padding: 16px;
}

body .comment-container .review-comment-header>span,
body .comment-container .review-comment-header>a,
body .commit .commit-message>a,
body .merged .merged-message>a {
	margin-right: 6px;
}

body .comment-container .review-comment-container .pending-label,
body .resolved-container .outdatedLabel {
	background: var(--vscode-badge-background);
	color: var(--vscode-badge-foreground);
	font-size: 11px;
	font-weight: 600;
	border-radius: 20px;
	padding: 4px 8px;
	margin-left: 6px;
}

body .resolved-container .unresolvedLabel {
	font-style: italic;
	margin-left: 5px;
}

body .diff .diffPath {
	margin-right: 4px;
}

.comment-container form,
#merge-comment-form {
	padding: 16px;
	background-color: var(--vscode-editorWidget-background);
}

body .comment-container .comment-body,
.review-body {
	padding: 16px;
	border-top: none;
}

body .comment-container .review-comment-container .review-comment-body {
	display: flex;
	flex-direction: column;
	gap: 16px;
	border: none;
}

body .comment-container .comment-body>p,
body .comment-container .comment-body>div>p,
.comment-container .review-body>p {
	margin-top: 0;
	line-height: 1.5em;
}

body .comment-container .comment-body>p:last-child,
body .comment-container .comment-body>div>p:last-child,
.comment-container .review-body>p:last-child {
	margin-bottom: 0;
}

body {
	margin: auto;
	width: 100%;
	max-width: 1280px;
	padding: 0 32px;
	box-sizing: border-box;
}

body .hidden-focusable {
	height: 0 !important;
	overflow: hidden;
}

.comment-actions button:hover:enabled,
.comment-actions button:focus:enabled {
	background-color: transparent;
}

body button.checkedOut {
	color: var(--vscode-foreground);
	opacity: 1 !important;
	background-color: transparent;
}

body button .icon {
	width: 16px;
	height: 16px;
}

.prIcon {
	display: flex;
	border-radius: 10px;
	margin-right: 5px;
	margin-top: 18px;
}

.overview-title h2 {
	font-size: 32px;
}

.overview-title textarea {
	min-height: 50px;
}

.title-container {
	width: 100%;
}

.subtitle {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	row-gap: 12px;
}

.subtitle .avatar,
.subtitle .avatar-icon svg {
	margin-right: 6px;
}

.subtitle .author {
	display: flex;
	align-items: center;
}

.merge-branches {
	display: inline-flex;
	align-items: center;
	gap: 4px;
	flex-wrap: wrap;
}

.branch-tag {
	padding: 2px 4px;
	background: var(--vscode-editorInlayHint-background);
	color: var(--vscode-editorInlayHint-foreground);
	border-radius: 4px;
}

.subtitle .created-at {
	margin-left: auto;
	white-space: nowrap;
}

.button-group {
	display: flex;
	gap: 8px;
	flex-wrap: wrap;
	align-items: flex-start;
}

.small-button {
	display: flex;
	font-size: 11px;
	padding: 0 5px;
}

.header-actions {
	display: flex;
	gap: 8px;
}

.header-actions>div:first-of-type {
	flex: 1;
}

:not(.status-item)>.small-button {
	font-weight: 600;
}

#status {
	box-sizing: border-box;
	line-height: 18px;
	color: var(--vscode-button-foreground);
	border-radius: 18px;
	padding: 4px 12px;
	margin-right: 10px;
	font-weight: 600;
	display: flex;
	gap: 4px;
}

#status svg path {
	fill: var(--vscode-button-foreground);
}

.vscode-high-contrast #status {
	border: 1px solid var(--vscode-contrastBorder);
	background-color: var(--vscode-badge-background);
	color: var(--vscode-badge-foreground);
}

.vscode-high-contrast #status svg path {
	fill: var(--vscode-badge-foreground);
}

.status-badge-merged {
	background-color: var(--vscode-pullRequests-merged);
}

.status-badge-open {
	background-color: var(--vscode-pullRequests-open);
}

.status-badge-closed {
	background-color: var(--vscode-pullRequests-closed);
}

.status-badge-draft {
	background-color: var(--vscode-pullRequests-draft);
}

.section {
	padding-bottom: 24px;
	border-bottom: 1px solid var(--vscode-editorWidget-border);
	display: flex;
	flex-direction: column;
	gap: 12px;
}

.section:last-of-type {
	padding-bottom: 0px;
	border-bottom: none;
}

.section-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	cursor: pointer;
}

.section-header .section-title {
	font-weight: 600;
}

.section-placeholder {
	color: var(--vscode-descriptionForeground);
}

.assign-yourself:hover {
	cursor: pointer;
}

.section svg {
	width: 16px;
	height: 16px;
	display: block;
	margin-right: 0;
}

.section .icon-button,
.section .icon-button .icon {
	color: currentColor;
}

.icon-button-group {
	display: flex;
	flex-direction: row;
}

.section svg path {
	fill: currentColor;
}

.commit svg {
	width: 16px;
	height: auto;
	margin-right: 8px;
	flex-shrink: 0;
}

.comment-container.commit {
	border: none;
	padding: 4px 16px;
}

.comment-container.commit,
.comment-container.merged {
	box-sizing: border-box;
}

.commit,
.review,
.merged {
	display: flex;
	width: 100%;
	border: none;
	color: var(--vscode-foreground);
}

.review {
	margin: 0px 8px;
	padding: 4px 0;
}

.commit .commit-message,
.commit .timeline-with-detail,
.merged .merged-message {
	align-items: center;
	overflow: hidden;
	flex-grow: 1;
}

.commit .commit-message,
.merged .merged-message {
	display: flex;
}

.commit .timeline-with-detail {
	display: block;
}

.commit-message-detail {
	margin-left: 20px;
}

.commit .commit-message .avatar-container,
.merged .merged-message .avatar-container {
	margin-right: 4px;
	flex-shrink: 0;
}

.commit-message .icon {
	padding-top: 2px;
}

.commit .avatar-container .avatar,
.commit .avatar-container .avatar-icon,
.commit .avatar-container .avatar-icon svg,
.merged .avatar-container .avatar,
.merged .avatar-container .avatar-icon,
.merged .avatar-container .avatar-icon svg {
	width: 18px;
	height: 18px;
}

.message-container {
	display: inline-grid;
}

.commit .commit-message .message,
.merged .merged-message .message {
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.timeline-detail {
	display: flex;
	align-items: center;
	gap: 8px;
}

.commit .sha {
	min-width: 50px;
	font-family: var(--vscode-editor-font-family);
	margin-bottom: -2px;
}

.merged .merged-message .message,
.merged .inline-sha {
	margin: 0 4px 0 0;
}

.merged svg {
	width: 14px;
	height: auto;
	margin-right: 8px;
	flex-shrink: 0;
}

.details {
	display: flex;
	flex-direction: column;
	gap: 12px;
	width: 100%;
}

#description .comment-container {
	padding-top: 0px;
}

.comment-container {
	position: relative;
	width: 100%;
	display: flex;
	margin: 0;
	align-items: center;
	border-radius: 4px;
	border: 1px solid var(--vscode-editorHoverWidget-border);
}

.comment-container[data-type='commit'] {
	padding: 8px 0;
	border: none;
}

.comment-container[data-type='commit']+.comment-container[data-type='commit'] {
	border-top: none;
}

.comment-body .review-comment {
	box-sizing: border-box;
	border-top: 1px solid var(--vscode-editorHoverWidget-border);
}

.resolve-comment-row {
	display: flex;
	align-items: center;
	padding: 16px;
	background-color: var(--vscode-editorHoverWidget-background);
	border-top: 1px solid var(--vscode-editorHoverWidget-border);
	border-bottom-left-radius: 3px;
	border-bottom-right-radius: 3px;
}

.review-comment-container .review-comment .review-comment-header {
	padding: 16px 16px 8px 16px;
	border: none;
	background: none;
}

.review-comment-container .review-comment .comment-body {
	border: none;
	padding: 0px 16px 8px 16px;
}

.review-comment-container .review-comment .comment-body:last-of-type {
	padding: 0px 16px 16px 16px;
}

.comment-body .line {
	align-items: center;
	display: flex;
	flex-wrap: wrap;
	margin-bottom: 8px;
}

body .comment-form {
	padding: 20px 0 10px;
}

.review-comment-container .comment-form {
	margin: 0 0 0 36px;
	padding: 10px 0;
}

.task-list-item {
	list-style-type: none;
}

#status-checks textarea {
	margin-top: 10px;
}

textarea {
	min-height: 100px;
	max-height: 500px;
}

.editing-form {
	padding: 5px 0;
	display: flex;
	flex-direction: column;
	min-width: 300px;
}

.editing-form .form-actions {
	display: flex;
	gap: 8px;
	justify-content: flex-end;
}

.comment-form .form-actions>button,
.comment-form .form-actions>input[type='submit'] {
	margin-right: 0;
	margin-left: 0;
}

.primary-split-button {
	flex-grow: unset;
}

.dropdown-container {
	justify-content: right;
}

.form-actions {
	display: flex;
	justify-content: flex-end;
	padding-top: 10px;
}

#rebase-actions {
	flex-direction: row-reverse;
}

.main-comment-form>.form-actions {
	margin-bottom: 10px;
}

.details .comment-body {
	padding: 19px 0;
}

blockquote {
	display: block;
	flex-direction: column;
	margin: 8px 0;
	padding: 8px 12px;
	border-left-width: 5px;
	border-left-style: solid;
}

blockquote p {
	margin: 8px 0;
}

blockquote p:first-child {
	margin-top: 0;
}

blockquote p:last-child {
	margin-bottom: 0;
}

.comment-body a:focus,
.comment-body input:focus,
.comment-body select:focus,
.comment-body textarea:focus {
	outline-offset: -1px;
}

.comment-body hr {
	border: 0;
	height: 2px;
	border-bottom: 2px solid;
}

.comment-body h1 {
	padding-bottom: 0.3em;
	line-height: 1.2;
	border-bottom-width: 1px;
	border-bottom-style: solid;
}

.comment-body h1,
h2,
h3 {
	font-weight: normal;
}

.comment-body h1 code,
.comment-body h2 code,
.comment-body h3 code,
.comment-body h4 code,
.comment-body h5 code,
.comment-body h6 code {
	font-size: inherit;
	line-height: auto;
}

.comment-body table {
	border-collapse: collapse;
}

.comment-body table>thead>tr>th {
	text-align: left;
	border-bottom: 1px solid;
}

.comment-body table>thead>tr>th,
.comment-body table>thead>tr>td,
.comment-body table>tbody>tr>th,
.comment-body table>tbody>tr>td {
	padding: 5px 10px;
}

.comment-body table>tbody>tr+tr>td {
	border-top: 1px solid;
}

code {
	font-family: var(--vscode-editor-font-family), Menlo, Monaco, Consolas, 'Droid Sans Mono', 'Courier New', monospace, 'Droid Sans Fallback';
}

.comment-body .snippet-clipboard-content {
	display: grid;
}

.comment-body video {
	width: 100%;
	border: 1px solid var(--vscode-editorWidget-border);
	border-radius: 4px;
}

.comment-body summary {
	margin-bottom: 8px;
}

.comment-body details summary::marker {
	display: flex;
}

.comment-body details summary svg {
	margin-left: 8px;
}

.comment-body body.wordWrap pre {
	white-space: pre-wrap;
}

.comment-body .mac code {
	font-size: 12px;
	line-height: 18px;
}

.comment-body pre:not(.hljs),
.comment-body pre.hljs code>div {
	padding: 16px;
	border-radius: 3px;
	overflow: auto;
}

.timestamp,
.timestamp:hover {
	color: var(--vscode-descriptionForeground);
	white-space: nowrap;
}

.timestamp {
	overflow: hidden;
	text-overflow: ellipsis;
	padding-left: 8px;
}

/** Theming */

.comment-body pre code {
	color: var(--vscode-editor-foreground);
}

.vscode-light .comment-body pre:not(.hljs),
.vscode-light .comment-body code>div {
	background-color: rgba(220, 220, 220, 0.4);
}

.vscode-dark .comment-body pre:not(.hljs),
.vscode-dark .comment-body code>div {
	background-color: rgba(10, 10, 10, 0.4);
}

.vscode-high-contrast .comment-body pre:not(.hljs),
.vscode-high-contrast .comment-body code>div {
	background-color: var(--vscode-editor-background);
	border: 1px solid var(--vscode-panel-border);
}

.vscode-high-contrast .comment-body h1 {
	border: 1px solid rgb(0, 0, 0);
}

.vscode-high-contrast .comment-container .review-comment-header,
.vscode-high-contrast #status-checks {
	background: none;
	border: 1px solid var(--vscode-panel-border);
}

.vscode-high-contrast .comment-container .comment-body,
.vscode-high-contrast .review-comment-container .review-body {
	border: 1px solid var(--vscode-panel-border);
}

.vscode-light .comment-body table>thead>tr>th {
	border-color: rgba(0, 0, 0, 0.69);
}

.vscode-dark .comment-body table>thead>tr>th {
	border-color: rgba(255, 255, 255, 0.69);
}

.vscode-light .comment-body h1,
.vscode-light .comment-body hr,
.vscode-light .comment-body table>tbody>tr+tr>td {
	border-color: rgba(0, 0, 0, 0.18);
}

.vscode-dark .comment-body h1,
.vscode-dark .comment-body hr,
.vscode-dark .comment-body table>tbody>tr+tr>td {
	border-color: rgba(255, 255, 255, 0.18);
}

.review-comment-body .diff-container {
	border-radius: 4px;
	border: 1px solid var(--vscode-editorHoverWidget-border);
}

.review-comment-body .diff-container .review-comment-container .comment-container {
	padding-top: 0;
}

.review-comment-body .diff-container .comment-container {
	border: none;
}

.review-comment-body .diff-container .review-comment-container .review-comment-header .avatar-container {
	margin-right: 4px;
}

.review-comment-body .diff-container .review-comment-container .review-comment-header .avatar {
	width: 18px;
	height: 18px;
}

.review-comment-body .diff-container .diff {
	border-top: 1px solid var(--vscode-editorWidget-border);
	overflow: scroll;
}

.resolved-container {
	padding: 6px 12px;
	display: flex;
	align-items: center;
	justify-content: space-between;
	background: var(--vscode-editorWidget-background);
	border-top-left-radius: 3px;
	border-top-right-radius: 3px;
}

.resolved-container .diffPath:hover {
	text-decoration: underline;
	color: var(--vscode-textLink-activeForeground);
	cursor: pointer;
}

.diff .diffLine {
	display: flex;
	font-size: 12px;
	line-height: 20px;
}

.win32 .diff .diffLine {
	font-family: var(--vscode-editor-font-family), Consolas, Inconsolata, 'Courier New', monospace;
}

.darwin .diff .diffLine {
	font-family: var(--vscode-editor-font-family), Monaco, Menlo, Inconsolata, 'Courier New', monospace;
}

.linux .diff .diffLine {
	font-family: var(--vscode-editor-font-family), 'Droid Sans Mono', Inconsolata, 'Courier New', monospace, 'Droid Sans Fallback';
}

.diff .diffLine.add {
	background-color: var(--vscode-diffEditor-insertedTextBackground);
}

.diff .diffLine.delete {
	background-color: var(--vscode-diffEditor-removedTextBackground);
}

.diff .diffLine .diffTypeSign {
	user-select: none;
	padding-right: 5px;
}

.diff .diffLine .lineNumber {
	width: 1%;
	min-width: 50px;
	padding-right: 10px;
	padding-left: 10px;
	font-size: 12px;
	line-height: 20px;
	text-align: right;
	white-space: nowrap;
	box-sizing: border-box;
	display: block;
	user-select: none;
	font-family: var(--vscode-editor-font-family);
}

.github-checkbox {
	pointer-events: none;
}

.github-checkbox input {
	color: rgb(84, 84, 84);
	opacity: 0.6;
}

/* High Contrast Mode */

.vscode-high-contrast a:focus {
	outline-color: var(--vscode-contrastActiveBorder);
}

.vscode-high-contrast .title {
	border-bottom: 1px solid var(--vscode-contrastBorder);
}

.vscode-high-contrast .diff .diffLine {
	background: none;
}

.vscode-high-contrast .resolved-container {
	background: none;
}

.vscode-high-contrast .diff-container {
	border: 1px solid var(--vscode-contrastBorder);
}

.vscode-high-contrast .diff .diffLine.add {
	border: 1px dashed var(--vscode-diffEditor-insertedTextBorder);
}

.vscode-high-contrast .diff .diffLine.delete {
	border: 1px dashed var(--vscode-diffEditor-removedTextBorder);
}

@media (max-width: 925px) {
	#app {
		display: block;
	}

	#sidebar {
		display: grid;
		column-gap: 20px;
		grid-template-columns: 50% 50%;
		padding: 0;
		padding-bottom: 24px;
	}

	.section-content {
		display: flex;
		flex-wrap: wrap;
	}

	.section-item {
		display: flex;
	}

	body .hidden-focusable {
		height: initial;
		overflow: initial;
	}

	.section-header button {
		margin-left: 8px;
		display: flex;
	}

	.section-item .login {
		width: auto;
		margin-right: 4px;
	}

	/* Hides bottom borders on bottom two sections */
	.section:nth-last-child(-n + 2) {
		border-bottom: none;
	}
}

.icon {
	width: 16px;
	height: 16px;
	font-size: 16px;
	display: flex;
}

.action-bar {
	position: absolute;
	display: flex;
	justify-content: space-between;
	z-index: 100;
	top: 9px;
	right: 9px;
}

.flex-action-bar {
	display: flex;
	justify-content: space-between;
	align-items: center;
	z-index: 100;
	margin-left: 9px;
	min-width: 42px;
}

.action-bar>button,
.flex-action-bar>button {
	margin-left: 4px;
	margin-right: 4px;
}

.title-editing-form {
	flex-grow: 1;
}

.title-editing-form>.form-actions {
	margin-left: 0;
}

/* permalinks */
.comment-body .Box p {
	margin-block-start: 0px;
	margin-block-end: 0px;
}

.comment-body .Box {
	border-radius: 4px;
	border-style: solid;
	border-width: 1px;
	border-color: var(--vscode-editorHoverWidget-border);
}

.comment-body .Box-header {
	background-color: var(--vscode-editorWidget-background);
	color: var(--vscode-disabledForeground);
	border-bottom-style: solid;
	border-bottom-width: 1px;
	padding: 8px 16px;
	border-bottom-color: var(--vscode-editorHoverWidget-border);
	border-top-left-radius: 3px;
	border-top-right-radius: 3px;
}

.comment-body .blob-num {
	word-wrap: break-word;
	box-sizing: border-box;
	border: 0 !important;
	padding-top: 0 !important;
	padding-bottom: 0 !important;
	min-width: 50px;
	font-family: var(--vscode-editor-font-family);
	font-size: 12px;
	color: var(--vscode-editorLineNumber-foreground);
	line-height: 20px;
	text-align: right;
	white-space: nowrap;
	vertical-align: top;
	cursor: pointer;
	user-select: none;
}

.comment-body .blob-num::before {
	content: attr(data-line-number);
}

.comment-body .blob-code-inner {
	tab-size: 8;
	border: 0 !important;
	padding-top: 0 !important;
	padding-bottom: 0 !important;
	line-height: 20px;
	vertical-align: top;
	display: table-cell;
	overflow: visible;
	font-family: var(--vscode-editor-font-family);
	font-size: 12px;
	word-wrap: anywhere;
	text-indent: 0;
	white-space: pre-wrap;
}

.comment-body .commit-tease-sha {
	font-family: var(--vscode-editor-font-family);
	font-size: 12px;
}

/* Suggestion */
.comment-body .blob-wrapper.data.file .d-table {
	border-radius: 4px;
	border-style: solid;
	border-width: 1px;
	border-collapse: unset;
	border-color: var(--vscode-editorHoverWidget-border);
}

.comment-body .js-suggested-changes-blob {
	border-collapse: collapse;
}

.blob-code-deletion,
.blob-num-deletion {
	border-collapse: collapse;
	background-color: var(--vscode-diffEditor-removedLineBackground);
}

.blob-code-addition,
.blob-num-addition {
	border-collapse: collapse;
	background-color: var(--vscode-diffEditor-insertedLineBackground);
}

.blob-code-marker-addition::before {
	content: "+ ";
}

.blob-code-marker-deletion::before {
	content: "- ";
}
`,""]);const y=I},76314:M=>{"use strict";M.exports=function(R){var J=[];return J.toString=i(function(){return this.map(function(le){var I=R(le);return le[2]?"@media ".concat(le[2]," {").concat(I,"}"):I}).join("")},"toString"),J.i=function(oe,le,I){typeof oe=="string"&&(oe=[[null,oe,""]]);var y={};if(I)for(var v=0;v<this.length;v++){var H=this[v][0];H!=null&&(y[H]=!0)}for(var z=0;z<oe.length;z++){var W=[].concat(oe[z]);I&&y[W[0]]||(le&&(W[2]?W[2]="".concat(le," and ").concat(W[2]):W[2]=le),J.push(W))}},J}},74353:function(M){(function(R,J){M.exports=J()})(this,function(){"use strict";var R="millisecond",J="second",oe="minute",le="hour",I="day",y="week",v="month",H="quarter",z="year",W="date",l=/^(\d{4})[-/]?(\d{1,2})?[-/]?(\d{0,2})[^0-9]*(\d{1,2})?:?(\d{1,2})?:?(\d{1,2})?[.:]?(\d+)?$/,ae=/\[([^\]]+)]|Y{1,4}|M{1,4}|D{1,2}|d{1,4}|H{1,2}|h{1,2}|a|A|m{1,2}|s{1,2}|Z{1,2}|SSS/g,G={name:"en",weekdays:"Sunday_Monday_Tuesday_Wednesday_Thursday_Friday_Saturday".split("_"),months:"January_February_March_April_May_June_July_August_September_October_November_December".split("_")},Oe=i(function(V,E,A){var ie=String(V);return!ie||ie.length>=E?V:""+Array(E+1-ie.length).join(A)+V},"$"),De={s:Oe,z:i(function(V){var E=-V.utcOffset(),A=Math.abs(E),ie=Math.floor(A/60),Q=A%60;return(E<=0?"+":"-")+Oe(ie,2,"0")+":"+Oe(Q,2,"0")},"z"),m:i(function V(E,A){if(E.date()<A.date())return-V(A,E);var ie=12*(A.year()-E.year())+(A.month()-E.month()),Q=E.clone().add(ie,v),B=A-Q<0,ge=E.clone().add(ie+(B?-1:1),v);return+(-(ie+(A-Q)/(B?Q-ge:ge-Q))||0)},"t"),a:i(function(V){return V<0?Math.ceil(V)||0:Math.floor(V)},"a"),p:i(function(V){return{M:v,y:z,w:y,d:I,D:W,h:le,m:oe,s:J,ms:R,Q:H}[V]||String(V||"").toLowerCase().replace(/s$/,"")},"p"),u:i(function(V){return V===void 0},"u")},$="en",Z={};Z[$]=G;var me=i(function(V){return V instanceof q},"m"),P=i(function(V,E,A){var ie;if(!V)return $;if(typeof V=="string")Z[V]&&(ie=V),E&&(Z[V]=E,ie=V);else{var Q=V.name;Z[Q]=V,ie=Q}return!A&&ie&&($=ie),ie||!A&&$},"D"),_=i(function(V,E){if(me(V))return V.clone();var A=typeof E=="object"?E:{};return A.date=V,A.args=arguments,new q(A)},"v"),T=De;T.l=P,T.i=me,T.w=function(V,E){return _(V,{locale:E.$L,utc:E.$u,x:E.$x,$offset:E.$offset})};var q=function(){function V(A){this.$L=P(A.locale,null,!0),this.parse(A)}i(V,"d");var E=V.prototype;return E.parse=function(A){this.$d=function(ie){var Q=ie.date,B=ie.utc;if(Q===null)return new Date(NaN);if(T.u(Q))return new Date;if(Q instanceof Date)return new Date(Q);if(typeof Q=="string"&&!/Z$/i.test(Q)){var ge=Q.match(l);if(ge){var ve=ge[2]-1||0,de=(ge[7]||"0").substring(0,3);return B?new Date(Date.UTC(ge[1],ve,ge[3]||1,ge[4]||0,ge[5]||0,ge[6]||0,de)):new Date(ge[1],ve,ge[3]||1,ge[4]||0,ge[5]||0,ge[6]||0,de)}}return new Date(Q)}(A),this.$x=A.x||{},this.init()},E.init=function(){var A=this.$d;this.$y=A.getFullYear(),this.$M=A.getMonth(),this.$D=A.getDate(),this.$W=A.getDay(),this.$H=A.getHours(),this.$m=A.getMinutes(),this.$s=A.getSeconds(),this.$ms=A.getMilliseconds()},E.$utils=function(){return T},E.isValid=function(){return this.$d.toString()!=="Invalid Date"},E.isSame=function(A,ie){var Q=_(A);return this.startOf(ie)<=Q&&Q<=this.endOf(ie)},E.isAfter=function(A,ie){return _(A)<this.startOf(ie)},E.isBefore=function(A,ie){return this.endOf(ie)<_(A)},E.$g=function(A,ie,Q){return T.u(A)?this[ie]:this.set(Q,A)},E.unix=function(){return Math.floor(this.valueOf()/1e3)},E.valueOf=function(){return this.$d.getTime()},E.startOf=function(A,ie){var Q=this,B=!!T.u(ie)||ie,ge=T.p(A),ve=i(function(ot,Fe){var F=T.w(Q.$u?Date.UTC(Q.$y,Fe,ot):new Date(Q.$y,Fe,ot),Q);return B?F:F.endOf(I)},"$"),de=i(function(ot,Fe){return T.w(Q.toDate()[ot].apply(Q.toDate("s"),(B?[0,0,0,0]:[23,59,59,999]).slice(Fe)),Q)},"l"),Ce=this.$W,Te=this.$M,Ze=this.$D,Qe="set"+(this.$u?"UTC":"");switch(ge){case z:return B?ve(1,0):ve(31,11);case v:return B?ve(1,Te):ve(0,Te+1);case y:var nt=this.$locale().weekStart||0,st=(Ce<nt?Ce+7:Ce)-nt;return ve(B?Ze-st:Ze+(6-st),Te);case I:case W:return de(Qe+"Hours",0);case le:return de(Qe+"Minutes",1);case oe:return de(Qe+"Seconds",2);case J:return de(Qe+"Milliseconds",3);default:return this.clone()}},E.endOf=function(A){return this.startOf(A,!1)},E.$set=function(A,ie){var Q,B=T.p(A),ge="set"+(this.$u?"UTC":""),ve=(Q={},Q[I]=ge+"Date",Q[W]=ge+"Date",Q[v]=ge+"Month",Q[z]=ge+"FullYear",Q[le]=ge+"Hours",Q[oe]=ge+"Minutes",Q[J]=ge+"Seconds",Q[R]=ge+"Milliseconds",Q)[B],de=B===I?this.$D+(ie-this.$W):ie;if(B===v||B===z){var Ce=this.clone().set(W,1);Ce.$d[ve](de),Ce.init(),this.$d=Ce.set(W,Math.min(this.$D,Ce.daysInMonth())).$d}else ve&&this.$d[ve](de);return this.init(),this},E.set=function(A,ie){return this.clone().$set(A,ie)},E.get=function(A){return this[T.p(A)]()},E.add=function(A,ie){var Q,B=this;A=Number(A);var ge=T.p(ie),ve=i(function(Te){var Ze=_(B);return T.w(Ze.date(Ze.date()+Math.round(Te*A)),B)},"d");if(ge===v)return this.set(v,this.$M+A);if(ge===z)return this.set(z,this.$y+A);if(ge===I)return ve(1);if(ge===y)return ve(7);var de=(Q={},Q[oe]=6e4,Q[le]=36e5,Q[J]=1e3,Q)[ge]||1,Ce=this.$d.getTime()+A*de;return T.w(Ce,this)},E.subtract=function(A,ie){return this.add(-1*A,ie)},E.format=function(A){var ie=this;if(!this.isValid())return"Invalid Date";var Q=A||"YYYY-MM-DDTHH:mm:ssZ",B=T.z(this),ge=this.$locale(),ve=this.$H,de=this.$m,Ce=this.$M,Te=ge.weekdays,Ze=ge.months,Qe=i(function(Fe,F,U,te){return Fe&&(Fe[F]||Fe(ie,Q))||U[F].substr(0,te)},"h"),nt=i(function(Fe){return T.s(ve%12||12,Fe,"0")},"d"),st=ge.meridiem||function(Fe,F,U){var te=Fe<12?"AM":"PM";return U?te.toLowerCase():te},ot={YY:String(this.$y).slice(-2),YYYY:this.$y,M:Ce+1,MM:T.s(Ce+1,2,"0"),MMM:Qe(ge.monthsShort,Ce,Ze,3),MMMM:Qe(Ze,Ce),D:this.$D,DD:T.s(this.$D,2,"0"),d:String(this.$W),dd:Qe(ge.weekdaysMin,this.$W,Te,2),ddd:Qe(ge.weekdaysShort,this.$W,Te,3),dddd:Te[this.$W],H:String(ve),HH:T.s(ve,2,"0"),h:nt(1),hh:nt(2),a:st(ve,de,!0),A:st(ve,de,!1),m:String(de),mm:T.s(de,2,"0"),s:String(this.$s),ss:T.s(this.$s,2,"0"),SSS:T.s(this.$ms,3,"0"),Z:B};return Q.replace(ae,function(Fe,F){return F||ot[Fe]||B.replace(":","")})},E.utcOffset=function(){return 15*-Math.round(this.$d.getTimezoneOffset()/15)},E.diff=function(A,ie,Q){var B,ge=T.p(ie),ve=_(A),de=6e4*(ve.utcOffset()-this.utcOffset()),Ce=this-ve,Te=T.m(this,ve);return Te=(B={},B[z]=Te/12,B[v]=Te,B[H]=Te/3,B[y]=(Ce-de)/6048e5,B[I]=(Ce-de)/864e5,B[le]=Ce/36e5,B[oe]=Ce/6e4,B[J]=Ce/1e3,B)[ge]||Ce,Q?Te:T.a(Te)},E.daysInMonth=function(){return this.endOf(v).$D},E.$locale=function(){return Z[this.$L]},E.locale=function(A,ie){if(!A)return this.$L;var Q=this.clone(),B=P(A,ie,!0);return B&&(Q.$L=B),Q},E.clone=function(){return T.w(this.$d,this)},E.toDate=function(){return new Date(this.valueOf())},E.toJSON=function(){return this.isValid()?this.toISOString():null},E.toISOString=function(){return this.$d.toISOString()},E.toString=function(){return this.$d.toUTCString()},V}(),ee=q.prototype;return _.prototype=ee,[["$ms",R],["$s",J],["$m",oe],["$H",le],["$W",I],["$M",v],["$y",z],["$D",W]].forEach(function(V){ee[V[1]]=function(E){return this.$g(E,V[0],V[1])}}),_.extend=function(V,E){return V.$i||(V(E,q,_),V.$i=!0),_},_.locale=P,_.isDayjs=me,_.unix=function(V){return _(1e3*V)},_.en=Z[$],_.Ls=Z,_.p={},_})},6279:function(M){(function(R,J){M.exports=J()})(this,function(){"use strict";return function(R,J,oe){R=R||{};var le=J.prototype,I={future:"in %s",past:"%s ago",s:"a few seconds",m:"a minute",mm:"%d minutes",h:"an hour",hh:"%d hours",d:"a day",dd:"%d days",M:"a month",MM:"%d months",y:"a year",yy:"%d years"};function y(H,z,W,l){return le.fromToBase(H,z,W,l)}i(y,"i"),oe.en.relativeTime=I,le.fromToBase=function(H,z,W,l,ae){for(var G,Oe,De,$=W.$locale().relativeTime||I,Z=R.thresholds||[{l:"s",r:44,d:"second"},{l:"m",r:89},{l:"mm",r:44,d:"minute"},{l:"h",r:89},{l:"hh",r:21,d:"hour"},{l:"d",r:35},{l:"dd",r:25,d:"day"},{l:"M",r:45},{l:"MM",r:10,d:"month"},{l:"y",r:17},{l:"yy",d:"year"}],me=Z.length,P=0;P<me;P+=1){var _=Z[P];_.d&&(G=l?oe(H).diff(W,_.d,!0):W.diff(H,_.d,!0));var T=(R.rounding||Math.round)(Math.abs(G));if(De=G>0,T<=_.r||!_.r){T<=1&&P>0&&(_=Z[P-1]);var q=$[_.l];ae&&(T=ae(""+T)),Oe=typeof q=="string"?q.replace("%d",T):q(T,z,_.l,De);break}}if(z)return Oe;var ee=De?$.future:$.past;return typeof ee=="function"?ee(Oe):ee.replace("%s",Oe)},le.to=function(H,z){return y(H,z,this,!0)},le.from=function(H,z){return y(H,z,this)};var v=i(function(H){return H.$u?oe.utc():oe()},"d");le.toNow=function(H){return this.to(v(this),H)},le.fromNow=function(H){return this.from(v(this),H)}}})},53581:function(M){(function(R,J){M.exports=J()})(this,function(){"use strict";return function(R,J,oe){oe.updateLocale=function(le,I){var y=oe.Ls[le];if(y)return(I?Object.keys(I):[]).forEach(function(v){y[v]=I[v]}),y}}})},17334:M=>{function R(J,oe,le){var I,y,v,H,z;oe==null&&(oe=100);function W(){var ae=Date.now()-H;ae<oe&&ae>=0?I=setTimeout(W,oe-ae):(I=null,le||(z=J.apply(v,y),v=y=null))}i(W,"later");var l=i(function(){v=this,y=arguments,H=Date.now();var ae=le&&!I;return I||(I=setTimeout(W,oe)),ae&&(z=J.apply(v,y),v=y=null),z},"debounced");return l.clear=function(){I&&(clearTimeout(I),I=null)},l.flush=function(){I&&(z=J.apply(v,y),v=y=null,clearTimeout(I),I=null)},l}i(R,"debounce"),R.debounce=R,M.exports=R},37007:M=>{"use strict";var R=typeof Reflect=="object"?Reflect:null,J=R&&typeof R.apply=="function"?R.apply:i(function(_,T,q){return Function.prototype.apply.call(_,T,q)},"ReflectApply"),oe;R&&typeof R.ownKeys=="function"?oe=R.ownKeys:Object.getOwnPropertySymbols?oe=i(function(_){return Object.getOwnPropertyNames(_).concat(Object.getOwnPropertySymbols(_))},"ReflectOwnKeys"):oe=i(function(_){return Object.getOwnPropertyNames(_)},"ReflectOwnKeys");function le(P){console&&console.warn&&console.warn(P)}i(le,"ProcessEmitWarning");var I=Number.isNaN||i(function(_){return _!==_},"NumberIsNaN");function y(){y.init.call(this)}i(y,"EventEmitter"),M.exports=y,M.exports.once=me,y.EventEmitter=y,y.prototype._events=void 0,y.prototype._eventsCount=0,y.prototype._maxListeners=void 0;var v=10;function H(P){if(typeof P!="function")throw new TypeError('The "listener" argument must be of type Function. Received type '+typeof P)}i(H,"checkListener"),Object.defineProperty(y,"defaultMaxListeners",{enumerable:!0,get:i(function(){return v},"get"),set:i(function(P){if(typeof P!="number"||P<0||I(P))throw new RangeError('The value of "defaultMaxListeners" is out of range. It must be a non-negative number. Received '+P+".");v=P},"set")}),y.init=function(){(this._events===void 0||this._events===Object.getPrototypeOf(this)._events)&&(this._events=Object.create(null),this._eventsCount=0),this._maxListeners=this._maxListeners||void 0},y.prototype.setMaxListeners=i(function(_){if(typeof _!="number"||_<0||I(_))throw new RangeError('The value of "n" is out of range. It must be a non-negative number. Received '+_+".");return this._maxListeners=_,this},"setMaxListeners");function z(P){return P._maxListeners===void 0?y.defaultMaxListeners:P._maxListeners}i(z,"_getMaxListeners"),y.prototype.getMaxListeners=i(function(){return z(this)},"getMaxListeners"),y.prototype.emit=i(function(_){for(var T=[],q=1;q<arguments.length;q++)T.push(arguments[q]);var ee=_==="error",V=this._events;if(V!==void 0)ee=ee&&V.error===void 0;else if(!ee)return!1;if(ee){var E;if(T.length>0&&(E=T[0]),E instanceof Error)throw E;var A=new Error("Unhandled error."+(E?" ("+E.message+")":""));throw A.context=E,A}var ie=V[_];if(ie===void 0)return!1;if(typeof ie=="function")J(ie,this,T);else for(var Q=ie.length,B=De(ie,Q),q=0;q<Q;++q)J(B[q],this,T);return!0},"emit");function W(P,_,T,q){var ee,V,E;if(H(T),V=P._events,V===void 0?(V=P._events=Object.create(null),P._eventsCount=0):(V.newListener!==void 0&&(P.emit("newListener",_,T.listener?T.listener:T),V=P._events),E=V[_]),E===void 0)E=V[_]=T,++P._eventsCount;else if(typeof E=="function"?E=V[_]=q?[T,E]:[E,T]:q?E.unshift(T):E.push(T),ee=z(P),ee>0&&E.length>ee&&!E.warned){E.warned=!0;var A=new Error("Possible EventEmitter memory leak detected. "+E.length+" "+String(_)+" listeners added. Use emitter.setMaxListeners() to increase limit");A.name="MaxListenersExceededWarning",A.emitter=P,A.type=_,A.count=E.length,le(A)}return P}i(W,"_addListener"),y.prototype.addListener=i(function(_,T){return W(this,_,T,!1)},"addListener"),y.prototype.on=y.prototype.addListener,y.prototype.prependListener=i(function(_,T){return W(this,_,T,!0)},"prependListener");function l(){if(!this.fired)return this.target.removeListener(this.type,this.wrapFn),this.fired=!0,arguments.length===0?this.listener.call(this.target):this.listener.apply(this.target,arguments)}i(l,"onceWrapper");function ae(P,_,T){var q={fired:!1,wrapFn:void 0,target:P,type:_,listener:T},ee=l.bind(q);return ee.listener=T,q.wrapFn=ee,ee}i(ae,"_onceWrap"),y.prototype.once=i(function(_,T){return H(T),this.on(_,ae(this,_,T)),this},"once"),y.prototype.prependOnceListener=i(function(_,T){return H(T),this.prependListener(_,ae(this,_,T)),this},"prependOnceListener"),y.prototype.removeListener=i(function(_,T){var q,ee,V,E,A;if(H(T),ee=this._events,ee===void 0)return this;if(q=ee[_],q===void 0)return this;if(q===T||q.listener===T)--this._eventsCount===0?this._events=Object.create(null):(delete ee[_],ee.removeListener&&this.emit("removeListener",_,q.listener||T));else if(typeof q!="function"){for(V=-1,E=q.length-1;E>=0;E--)if(q[E]===T||q[E].listener===T){A=q[E].listener,V=E;break}if(V<0)return this;V===0?q.shift():$(q,V),q.length===1&&(ee[_]=q[0]),ee.removeListener!==void 0&&this.emit("removeListener",_,A||T)}return this},"removeListener"),y.prototype.off=y.prototype.removeListener,y.prototype.removeAllListeners=i(function(_){var T,q,ee;if(q=this._events,q===void 0)return this;if(q.removeListener===void 0)return arguments.length===0?(this._events=Object.create(null),this._eventsCount=0):q[_]!==void 0&&(--this._eventsCount===0?this._events=Object.create(null):delete q[_]),this;if(arguments.length===0){var V=Object.keys(q),E;for(ee=0;ee<V.length;++ee)E=V[ee],E!=="removeListener"&&this.removeAllListeners(E);return this.removeAllListeners("removeListener"),this._events=Object.create(null),this._eventsCount=0,this}if(T=q[_],typeof T=="function")this.removeListener(_,T);else if(T!==void 0)for(ee=T.length-1;ee>=0;ee--)this.removeListener(_,T[ee]);return this},"removeAllListeners");function G(P,_,T){var q=P._events;if(q===void 0)return[];var ee=q[_];return ee===void 0?[]:typeof ee=="function"?T?[ee.listener||ee]:[ee]:T?Z(ee):De(ee,ee.length)}i(G,"_listeners"),y.prototype.listeners=i(function(_){return G(this,_,!0)},"listeners"),y.prototype.rawListeners=i(function(_){return G(this,_,!1)},"rawListeners"),y.listenerCount=function(P,_){return typeof P.listenerCount=="function"?P.listenerCount(_):Oe.call(P,_)},y.prototype.listenerCount=Oe;function Oe(P){var _=this._events;if(_!==void 0){var T=_[P];if(typeof T=="function")return 1;if(T!==void 0)return T.length}return 0}i(Oe,"listenerCount"),y.prototype.eventNames=i(function(){return this._eventsCount>0?oe(this._events):[]},"eventNames");function De(P,_){for(var T=new Array(_),q=0;q<_;++q)T[q]=P[q];return T}i(De,"arrayClone");function $(P,_){for(;_+1<P.length;_++)P[_]=P[_+1];P.pop()}i($,"spliceOne");function Z(P){for(var _=new Array(P.length),T=0;T<_.length;++T)_[T]=P[T].listener||P[T];return _}i(Z,"unwrapListeners");function me(P,_){return new Promise(function(T,q){function ee(){V!==void 0&&P.removeListener("error",V),T([].slice.call(arguments))}i(ee,"eventListener");var V;_!=="error"&&(V=i(function(A){P.removeListener(_,ee),q(A)},"errorListener"),P.once("error",V)),P.once(_,ee)})}i(me,"once")},45228:M=>{"use strict";/*
object-assign
(c) Sindre Sorhus
@license MIT
*/var R=Object.getOwnPropertySymbols,J=Object.prototype.hasOwnProperty,oe=Object.prototype.propertyIsEnumerable;function le(y){if(y==null)throw new TypeError("Object.assign cannot be called with null or undefined");return Object(y)}i(le,"toObject");function I(){try{if(!Object.assign)return!1;var y=new String("abc");if(y[5]="de",Object.getOwnPropertyNames(y)[0]==="5")return!1;for(var v={},H=0;H<10;H++)v["_"+String.fromCharCode(H)]=H;var z=Object.getOwnPropertyNames(v).map(function(l){return v[l]});if(z.join("")!=="0123456789")return!1;var W={};return"abcdefghijklmnopqrst".split("").forEach(function(l){W[l]=l}),Object.keys(Object.assign({},W)).join("")==="abcdefghijklmnopqrst"}catch{return!1}}i(I,"shouldUseNative"),M.exports=I()?Object.assign:function(y,v){for(var H,z=le(y),W,l=1;l<arguments.length;l++){H=Object(arguments[l]);for(var ae in H)J.call(H,ae)&&(z[ae]=H[ae]);if(R){W=R(H);for(var G=0;G<W.length;G++)oe.call(H,W[G])&&(z[W[G]]=H[W[G]])}}return z}},57975:M=>{"use strict";function R(I){if(typeof I!="string")throw new TypeError("Path must be a string. Received "+JSON.stringify(I))}i(R,"assertPath");function J(I,y){for(var v="",H=0,z=-1,W=0,l,ae=0;ae<=I.length;++ae){if(ae<I.length)l=I.charCodeAt(ae);else{if(l===47)break;l=47}if(l===47){if(!(z===ae-1||W===1))if(z!==ae-1&&W===2){if(v.length<2||H!==2||v.charCodeAt(v.length-1)!==46||v.charCodeAt(v.length-2)!==46){if(v.length>2){var G=v.lastIndexOf("/");if(G!==v.length-1){G===-1?(v="",H=0):(v=v.slice(0,G),H=v.length-1-v.lastIndexOf("/")),z=ae,W=0;continue}}else if(v.length===2||v.length===1){v="",H=0,z=ae,W=0;continue}}y&&(v.length>0?v+="/..":v="..",H=2)}else v.length>0?v+="/"+I.slice(z+1,ae):v=I.slice(z+1,ae),H=ae-z-1;z=ae,W=0}else l===46&&W!==-1?++W:W=-1}return v}i(J,"normalizeStringPosix");function oe(I,y){var v=y.dir||y.root,H=y.base||(y.name||"")+(y.ext||"");return v?v===y.root?v+H:v+I+H:H}i(oe,"_format");var le={resolve:i(function(){for(var y="",v=!1,H,z=arguments.length-1;z>=-1&&!v;z--){var W;z>=0?W=arguments[z]:(H===void 0&&(H=process.cwd()),W=H),R(W),W.length!==0&&(y=W+"/"+y,v=W.charCodeAt(0)===47)}return y=J(y,!v),v?y.length>0?"/"+y:"/":y.length>0?y:"."},"resolve"),normalize:i(function(y){if(R(y),y.length===0)return".";var v=y.charCodeAt(0)===47,H=y.charCodeAt(y.length-1)===47;return y=J(y,!v),y.length===0&&!v&&(y="."),y.length>0&&H&&(y+="/"),v?"/"+y:y},"normalize"),isAbsolute:i(function(y){return R(y),y.length>0&&y.charCodeAt(0)===47},"isAbsolute"),join:i(function(){if(arguments.length===0)return".";for(var y,v=0;v<arguments.length;++v){var H=arguments[v];R(H),H.length>0&&(y===void 0?y=H:y+="/"+H)}return y===void 0?".":le.normalize(y)},"join"),relative:i(function(y,v){if(R(y),R(v),y===v||(y=le.resolve(y),v=le.resolve(v),y===v))return"";for(var H=1;H<y.length&&y.charCodeAt(H)===47;++H);for(var z=y.length,W=z-H,l=1;l<v.length&&v.charCodeAt(l)===47;++l);for(var ae=v.length,G=ae-l,Oe=W<G?W:G,De=-1,$=0;$<=Oe;++$){if($===Oe){if(G>Oe){if(v.charCodeAt(l+$)===47)return v.slice(l+$+1);if($===0)return v.slice(l+$)}else W>Oe&&(y.charCodeAt(H+$)===47?De=$:$===0&&(De=0));break}var Z=y.charCodeAt(H+$),me=v.charCodeAt(l+$);if(Z!==me)break;Z===47&&(De=$)}var P="";for($=H+De+1;$<=z;++$)($===z||y.charCodeAt($)===47)&&(P.length===0?P+="..":P+="/..");return P.length>0?P+v.slice(l+De):(l+=De,v.charCodeAt(l)===47&&++l,v.slice(l))},"relative"),_makeLong:i(function(y){return y},"_makeLong"),dirname:i(function(y){if(R(y),y.length===0)return".";for(var v=y.charCodeAt(0),H=v===47,z=-1,W=!0,l=y.length-1;l>=1;--l)if(v=y.charCodeAt(l),v===47){if(!W){z=l;break}}else W=!1;return z===-1?H?"/":".":H&&z===1?"//":y.slice(0,z)},"dirname"),basename:i(function(y,v){if(v!==void 0&&typeof v!="string")throw new TypeError('"ext" argument must be a string');R(y);var H=0,z=-1,W=!0,l;if(v!==void 0&&v.length>0&&v.length<=y.length){if(v.length===y.length&&v===y)return"";var ae=v.length-1,G=-1;for(l=y.length-1;l>=0;--l){var Oe=y.charCodeAt(l);if(Oe===47){if(!W){H=l+1;break}}else G===-1&&(W=!1,G=l+1),ae>=0&&(Oe===v.charCodeAt(ae)?--ae===-1&&(z=l):(ae=-1,z=G))}return H===z?z=G:z===-1&&(z=y.length),y.slice(H,z)}else{for(l=y.length-1;l>=0;--l)if(y.charCodeAt(l)===47){if(!W){H=l+1;break}}else z===-1&&(W=!1,z=l+1);return z===-1?"":y.slice(H,z)}},"basename"),extname:i(function(y){R(y);for(var v=-1,H=0,z=-1,W=!0,l=0,ae=y.length-1;ae>=0;--ae){var G=y.charCodeAt(ae);if(G===47){if(!W){H=ae+1;break}continue}z===-1&&(W=!1,z=ae+1),G===46?v===-1?v=ae:l!==1&&(l=1):v!==-1&&(l=-1)}return v===-1||z===-1||l===0||l===1&&v===z-1&&v===H+1?"":y.slice(v,z)},"extname"),format:i(function(y){if(y===null||typeof y!="object")throw new TypeError('The "pathObject" argument must be of type Object. Received type '+typeof y);return oe("/",y)},"format"),parse:i(function(y){R(y);var v={root:"",dir:"",base:"",ext:"",name:""};if(y.length===0)return v;var H=y.charCodeAt(0),z=H===47,W;z?(v.root="/",W=1):W=0;for(var l=-1,ae=0,G=-1,Oe=!0,De=y.length-1,$=0;De>=W;--De){if(H=y.charCodeAt(De),H===47){if(!Oe){ae=De+1;break}continue}G===-1&&(Oe=!1,G=De+1),H===46?l===-1?l=De:$!==1&&($=1):l!==-1&&($=-1)}return l===-1||G===-1||$===0||$===1&&l===G-1&&l===ae+1?G!==-1&&(ae===0&&z?v.base=v.name=y.slice(1,G):v.base=v.name=y.slice(ae,G)):(ae===0&&z?(v.name=y.slice(1,l),v.base=y.slice(1,G)):(v.name=y.slice(ae,l),v.base=y.slice(ae,G)),v.ext=y.slice(l,G)),ae>0?v.dir=y.slice(0,ae-1):z&&(v.dir="/"),v},"parse"),sep:"/",delimiter:":",win32:null,posix:null};le.posix=le,M.exports=le},22551:(M,R,J)=>{"use strict";var oe;/** @license React v16.14.0
 * react-dom.production.min.js
 *
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */var le=J(96540),I=J(45228),y=J(69982);function v(e){for(var t="https://reactjs.org/docs/error-decoder.html?invariant="+e,n=1;n<arguments.length;n++)t+="&args[]="+encodeURIComponent(arguments[n]);return"Minified React error #"+e+"; visit "+t+" for the full message or use the non-minified dev environment for full errors and additional helpful warnings."}if(i(v,"u"),!le)throw Error(v(227));function H(e,t,n,r,s,c,m,h,L){var S=Array.prototype.slice.call(arguments,3);try{t.apply(n,S)}catch(re){this.onError(re)}}i(H,"ba");var z=!1,W=null,l=!1,ae=null,G={onError:i(function(e){z=!0,W=e},"onError")};function Oe(e,t,n,r,s,c,m,h,L){z=!1,W=null,H.apply(G,arguments)}i(Oe,"ja");function De(e,t,n,r,s,c,m,h,L){if(Oe.apply(this,arguments),z){if(z){var S=W;z=!1,W=null}else throw Error(v(198));l||(l=!0,ae=S)}}i(De,"ka");var $=null,Z=null,me=null;function P(e,t,n){var r=e.type||"unknown-event";e.currentTarget=me(n),De(r,t,void 0,e),e.currentTarget=null}i(P,"oa");var _=null,T={};function q(){if(_)for(var e in T){var t=T[e],n=_.indexOf(e);if(!(-1<n))throw Error(v(96,e));if(!V[n]){if(!t.extractEvents)throw Error(v(97,e));V[n]=t,n=t.eventTypes;for(var r in n){var s=void 0,c=n[r],m=t,h=r;if(E.hasOwnProperty(h))throw Error(v(99,h));E[h]=c;var L=c.phasedRegistrationNames;if(L){for(s in L)L.hasOwnProperty(s)&&ee(L[s],m,h);s=!0}else c.registrationName?(ee(c.registrationName,m,h),s=!0):s=!1;if(!s)throw Error(v(98,r,e))}}}}i(q,"ra");function ee(e,t,n){if(A[e])throw Error(v(100,e));A[e]=t,ie[e]=t.eventTypes[n].dependencies}i(ee,"ua");var V=[],E={},A={},ie={};function Q(e){var t=!1,n;for(n in e)if(e.hasOwnProperty(n)){var r=e[n];if(!T.hasOwnProperty(n)||T[n]!==r){if(T[n])throw Error(v(102,n));T[n]=r,t=!0}}t&&q()}i(Q,"xa");var B=!(typeof window=="undefined"||typeof window.document=="undefined"||typeof window.document.createElement=="undefined"),ge=null,ve=null,de=null;function Ce(e){if(e=Z(e)){if(typeof ge!="function")throw Error(v(280));var t=e.stateNode;t&&(t=$(t),ge(e.stateNode,e.type,t))}}i(Ce,"Ca");function Te(e){ve?de?de.push(e):de=[e]:ve=e}i(Te,"Da");function Ze(){if(ve){var e=ve,t=de;if(de=ve=null,Ce(e),t)for(e=0;e<t.length;e++)Ce(t[e])}}i(Ze,"Ea");function Qe(e,t){return e(t)}i(Qe,"Fa");function nt(e,t,n,r,s){return e(t,n,r,s)}i(nt,"Ga");function st(){}i(st,"Ha");var ot=Qe,Fe=!1,F=!1;function U(){(ve!==null||de!==null)&&(st(),Ze())}i(U,"La");function te(e,t,n){if(F)return e(t,n);F=!0;try{return ot(e,t,n)}finally{F=!1,U()}}i(te,"Ma");var w=/^[:A-Z_a-z\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u02FF\u0370-\u037D\u037F-\u1FFF\u200C-\u200D\u2070-\u218F\u2C00-\u2FEF\u3001-\uD7FF\uF900-\uFDCF\uFDF0-\uFFFD][:A-Z_a-z\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u02FF\u0370-\u037D\u037F-\u1FFF\u200C-\u200D\u2070-\u218F\u2C00-\u2FEF\u3001-\uD7FF\uF900-\uFDCF\uFDF0-\uFFFD\-.0-9\u00B7\u0300-\u036F\u203F-\u2040]*$/,O=Object.prototype.hasOwnProperty,he={},Ee={};function we(e){return O.call(Ee,e)?!0:O.call(he,e)?!1:w.test(e)?Ee[e]=!0:(he[e]=!0,!1)}i(we,"Ra");function se(e,t,n,r){if(n!==null&&n.type===0)return!1;switch(typeof t){case"function":case"symbol":return!0;case"boolean":return r?!1:n!==null?!n.acceptsBooleans:(e=e.toLowerCase().slice(0,5),e!=="data-"&&e!=="aria-");default:return!1}}i(se,"Sa");function pt(e,t,n,r){if(t===null||typeof t=="undefined"||se(e,t,n,r))return!0;if(r)return!1;if(n!==null)switch(n.type){case 3:return!t;case 4:return t===!1;case 5:return isNaN(t);case 6:return isNaN(t)||1>t}return!1}i(pt,"Ta");function ke(e,t,n,r,s,c){this.acceptsBooleans=t===2||t===3||t===4,this.attributeName=r,this.attributeNamespace=s,this.mustUseProperty=n,this.propertyName=e,this.type=t,this.sanitizeURL=c}i(ke,"v");var Se={};"children dangerouslySetInnerHTML defaultValue defaultChecked innerHTML suppressContentEditableWarning suppressHydrationWarning style".split(" ").forEach(function(e){Se[e]=new ke(e,0,!1,e,null,!1)}),[["acceptCharset","accept-charset"],["className","class"],["htmlFor","for"],["httpEquiv","http-equiv"]].forEach(function(e){var t=e[0];Se[t]=new ke(t,1,!1,e[1],null,!1)}),["contentEditable","draggable","spellCheck","value"].forEach(function(e){Se[e]=new ke(e,2,!1,e.toLowerCase(),null,!1)}),["autoReverse","externalResourcesRequired","focusable","preserveAlpha"].forEach(function(e){Se[e]=new ke(e,2,!1,e,null,!1)}),"allowFullScreen async autoFocus autoPlay controls default defer disabled disablePictureInPicture formNoValidate hidden loop noModule noValidate open playsInline readOnly required reversed scoped seamless itemScope".split(" ").forEach(function(e){Se[e]=new ke(e,3,!1,e.toLowerCase(),null,!1)}),["checked","multiple","muted","selected"].forEach(function(e){Se[e]=new ke(e,3,!0,e,null,!1)}),["capture","download"].forEach(function(e){Se[e]=new ke(e,4,!1,e,null,!1)}),["cols","rows","size","span"].forEach(function(e){Se[e]=new ke(e,6,!1,e,null,!1)}),["rowSpan","start"].forEach(function(e){Se[e]=new ke(e,5,!1,e.toLowerCase(),null,!1)});var ht=/[\-:]([a-z])/g;function jr(e){return e[1].toUpperCase()}i(jr,"Va"),"accent-height alignment-baseline arabic-form baseline-shift cap-height clip-path clip-rule color-interpolation color-interpolation-filters color-profile color-rendering dominant-baseline enable-background fill-opacity fill-rule flood-color flood-opacity font-family font-size font-size-adjust font-stretch font-style font-variant font-weight glyph-name glyph-orientation-horizontal glyph-orientation-vertical horiz-adv-x horiz-origin-x image-rendering letter-spacing lighting-color marker-end marker-mid marker-start overline-position overline-thickness paint-order panose-1 pointer-events rendering-intent shape-rendering stop-color stop-opacity strikethrough-position strikethrough-thickness stroke-dasharray stroke-dashoffset stroke-linecap stroke-linejoin stroke-miterlimit stroke-opacity stroke-width text-anchor text-decoration text-rendering underline-position underline-thickness unicode-bidi unicode-range units-per-em v-alphabetic v-hanging v-ideographic v-mathematical vector-effect vert-adv-y vert-origin-x vert-origin-y word-spacing writing-mode xmlns:xlink x-height".split(" ").forEach(function(e){var t=e.replace(ht,jr);Se[t]=new ke(t,1,!1,e,null,!1)}),"xlink:actuate xlink:arcrole xlink:role xlink:show xlink:title xlink:type".split(" ").forEach(function(e){var t=e.replace(ht,jr);Se[t]=new ke(t,1,!1,e,"http://www.w3.org/1999/xlink",!1)}),["xml:base","xml:lang","xml:space"].forEach(function(e){var t=e.replace(ht,jr);Se[t]=new ke(t,1,!1,e,"http://www.w3.org/XML/1998/namespace",!1)}),["tabIndex","crossOrigin"].forEach(function(e){Se[e]=new ke(e,1,!1,e.toLowerCase(),null,!1)}),Se.xlinkHref=new ke("xlinkHref",1,!1,"xlink:href","http://www.w3.org/1999/xlink",!0),["src","href","action","formAction"].forEach(function(e){Se[e]=new ke(e,1,!1,e.toLowerCase(),null,!0)});var xt=le.__SECRET_INTERNALS_DO_NOT_USE_OR_YOU_WILL_BE_FIRED;xt.hasOwnProperty("ReactCurrentDispatcher")||(xt.ReactCurrentDispatcher={current:null}),xt.hasOwnProperty("ReactCurrentBatchConfig")||(xt.ReactCurrentBatchConfig={suspense:null});function Br(e,t,n,r){var s=Se.hasOwnProperty(t)?Se[t]:null,c=s!==null?s.type===0:r?!1:!(!(2<t.length)||t[0]!=="o"&&t[0]!=="O"||t[1]!=="n"&&t[1]!=="N");c||(pt(t,n,s,r)&&(n=null),r||s===null?we(t)&&(n===null?e.removeAttribute(t):e.setAttribute(t,""+n)):s.mustUseProperty?e[s.propertyName]=n===null?s.type===3?!1:"":n:(t=s.attributeName,r=s.attributeNamespace,n===null?e.removeAttribute(t):(s=s.type,n=s===3||s===4&&n===!0?"":""+n,r?e.setAttributeNS(r,t,n):e.setAttribute(t,n))))}i(Br,"Xa");var Hl=/^(.*)[\\\/]/,dt=typeof Symbol=="function"&&Symbol.for,Ur=dt?Symbol.for("react.element"):60103,cn=dt?Symbol.for("react.portal"):60106,Pt=dt?Symbol.for("react.fragment"):60107,Fl=dt?Symbol.for("react.strict_mode"):60108,fr=dt?Symbol.for("react.profiler"):60114,si=dt?Symbol.for("react.provider"):60109,ai=dt?Symbol.for("react.context"):60110,zl=dt?Symbol.for("react.concurrent_mode"):60111,Wr=dt?Symbol.for("react.forward_ref"):60112,dn=dt?Symbol.for("react.suspense"):60113,mr=dt?Symbol.for("react.suspense_list"):60120,en=dt?Symbol.for("react.memo"):60115,Ot=dt?Symbol.for("react.lazy"):60116,ui=dt?Symbol.for("react.block"):60121,Vl=typeof Symbol=="function"&&Symbol.iterator;function pr(e){return e===null||typeof e!="object"?null:(e=Vl&&e[Vl]||e["@@iterator"],typeof e=="function"?e:null)}i(pr,"nb");function pa(e){if(e._status===-1){e._status=0;var t=e._ctor;t=t(),e._result=t,t.then(function(n){e._status===0&&(n=n.default,e._status=1,e._result=n)},function(n){e._status===0&&(e._status=2,e._result=n)})}}i(pa,"ob");function jt(e){if(e==null)return null;if(typeof e=="function")return e.displayName||e.name||null;if(typeof e=="string")return e;switch(e){case Pt:return"Fragment";case cn:return"Portal";case fr:return"Profiler";case Fl:return"StrictMode";case dn:return"Suspense";case mr:return"SuspenseList"}if(typeof e=="object")switch(e.$$typeof){case ai:return"Context.Consumer";case si:return"Context.Provider";case Wr:var t=e.render;return t=t.displayName||t.name||"",e.displayName||(t!==""?"ForwardRef("+t+")":"ForwardRef");case en:return jt(e.type);case ui:return jt(e.render);case Ot:if(e=e._status===1?e._result:null)return jt(e)}return null}i(jt,"pb");function ci(e){var t="";do{e:switch(e.tag){case 3:case 4:case 6:case 7:case 10:case 9:var n="";break e;default:var r=e._debugOwner,s=e._debugSource,c=jt(e.type);n=null,r&&(n=jt(r.type)),r=c,c="",s?c=" (at "+s.fileName.replace(Hl,"")+":"+s.lineNumber+")":n&&(c=" (created by "+n+")"),n=`
    in `+(r||"Unknown")+c}t+=n,e=e.return}while(e);return t}i(ci,"qb");function tn(e){switch(typeof e){case"boolean":case"number":case"object":case"string":case"undefined":return e;default:return""}}i(tn,"rb");function $l(e){var t=e.type;return(e=e.nodeName)&&e.toLowerCase()==="input"&&(t==="checkbox"||t==="radio")}i($l,"sb");function ha(e){var t=$l(e)?"checked":"value",n=Object.getOwnPropertyDescriptor(e.constructor.prototype,t),r=""+e[t];if(!e.hasOwnProperty(t)&&typeof n!="undefined"&&typeof n.get=="function"&&typeof n.set=="function"){var s=n.get,c=n.set;return Object.defineProperty(e,t,{configurable:!0,get:i(function(){return s.call(this)},"get"),set:i(function(m){r=""+m,c.call(this,m)},"set")}),Object.defineProperty(e,t,{enumerable:n.enumerable}),{getValue:i(function(){return r},"getValue"),setValue:i(function(m){r=""+m},"setValue"),stopTracking:i(function(){e._valueTracker=null,delete e[t]},"stopTracking")}}}i(ha,"tb");function qr(e){e._valueTracker||(e._valueTracker=ha(e))}i(qr,"xb");function jl(e){if(!e)return!1;var t=e._valueTracker;if(!t)return!0;var n=t.getValue(),r="";return e&&(r=$l(e)?e.checked?"true":"false":e.value),e=r,e!==n?(t.setValue(e),!0):!1}i(jl,"yb");function Zr(e,t){var n=t.checked;return I({},t,{defaultChecked:void 0,defaultValue:void 0,value:void 0,checked:n!=null?n:e._wrapperState.initialChecked})}i(Zr,"zb");function di(e,t){var n=t.defaultValue==null?"":t.defaultValue,r=t.checked!=null?t.checked:t.defaultChecked;n=tn(t.value!=null?t.value:n),e._wrapperState={initialChecked:r,initialValue:n,controlled:t.type==="checkbox"||t.type==="radio"?t.checked!=null:t.value!=null}}i(di,"Ab");function fi(e,t){t=t.checked,t!=null&&Br(e,"checked",t,!1)}i(fi,"Bb");function Qr(e,t){fi(e,t);var n=tn(t.value),r=t.type;if(n!=null)r==="number"?(n===0&&e.value===""||e.value!=n)&&(e.value=""+n):e.value!==""+n&&(e.value=""+n);else if(r==="submit"||r==="reset"){e.removeAttribute("value");return}t.hasOwnProperty("value")?Kr(e,t.type,n):t.hasOwnProperty("defaultValue")&&Kr(e,t.type,tn(t.defaultValue)),t.checked==null&&t.defaultChecked!=null&&(e.defaultChecked=!!t.defaultChecked)}i(Qr,"Cb");function mi(e,t,n){if(t.hasOwnProperty("value")||t.hasOwnProperty("defaultValue")){var r=t.type;if(!(r!=="submit"&&r!=="reset"||t.value!==void 0&&t.value!==null))return;t=""+e._wrapperState.initialValue,n||t===e.value||(e.value=t),e.defaultValue=t}n=e.name,n!==""&&(e.name=""),e.defaultChecked=!!e._wrapperState.initialChecked,n!==""&&(e.name=n)}i(mi,"Eb");function Kr(e,t,n){(t!=="number"||e.ownerDocument.activeElement!==e)&&(n==null?e.defaultValue=""+e._wrapperState.initialValue:e.defaultValue!==""+n&&(e.defaultValue=""+n))}i(Kr,"Db");function Bl(e){var t="";return le.Children.forEach(e,function(n){n!=null&&(t+=n)}),t}i(Bl,"Fb");function Yr(e,t){return e=I({children:void 0},t),(t=Bl(t.children))&&(e.children=t),e}i(Yr,"Gb");function Ue(e,t,n,r){if(e=e.options,t){t={};for(var s=0;s<n.length;s++)t["$"+n[s]]=!0;for(n=0;n<e.length;n++)s=t.hasOwnProperty("$"+e[n].value),e[n].selected!==s&&(e[n].selected=s),s&&r&&(e[n].defaultSelected=!0)}else{for(n=""+tn(n),t=null,s=0;s<e.length;s++){if(e[s].value===n){e[s].selected=!0,r&&(e[s].defaultSelected=!0);return}t!==null||e[s].disabled||(t=e[s])}t!==null&&(t.selected=!0)}}i(Ue,"Hb");function Gr(e,t){if(t.dangerouslySetInnerHTML!=null)throw Error(v(91));return I({},t,{value:void 0,defaultValue:void 0,children:""+e._wrapperState.initialValue})}i(Gr,"Ib");function Ul(e,t){var n=t.value;if(n==null){if(n=t.children,t=t.defaultValue,n!=null){if(t!=null)throw Error(v(92));if(Array.isArray(n)){if(!(1>=n.length))throw Error(v(93));n=n[0]}t=n}t==null&&(t=""),n=t}e._wrapperState={initialValue:tn(n)}}i(Ul,"Jb");function pi(e,t){var n=tn(t.value),r=tn(t.defaultValue);n!=null&&(n=""+n,n!==e.value&&(e.value=n),t.defaultValue==null&&e.defaultValue!==n&&(e.defaultValue=n)),r!=null&&(e.defaultValue=""+r)}i(pi,"Kb");function On(e){var t=e.textContent;t===e._wrapperState.initialValue&&t!==""&&t!==null&&(e.value=t)}i(On,"Lb");var hi={html:"http://www.w3.org/1999/xhtml",mathml:"http://www.w3.org/1998/Math/MathML",svg:"http://www.w3.org/2000/svg"};function vi(e){switch(e){case"svg":return"http://www.w3.org/2000/svg";case"math":return"http://www.w3.org/1998/Math/MathML";default:return"http://www.w3.org/1999/xhtml"}}i(vi,"Nb");function Xr(e,t){return e==null||e==="http://www.w3.org/1999/xhtml"?vi(t):e==="http://www.w3.org/2000/svg"&&t==="foreignObject"?"http://www.w3.org/1999/xhtml":e}i(Xr,"Ob");var hr,gi=function(e){return typeof MSApp!="undefined"&&MSApp.execUnsafeLocalFunction?function(t,n,r,s){MSApp.execUnsafeLocalFunction(function(){return e(t,n,r,s)})}:e}(function(e,t){if(e.namespaceURI!==hi.svg||"innerHTML"in e)e.innerHTML=t;else{for(hr=hr||document.createElement("div"),hr.innerHTML="<svg>"+t.valueOf().toString()+"</svg>",t=hr.firstChild;e.firstChild;)e.removeChild(e.firstChild);for(;t.firstChild;)e.appendChild(t.firstChild)}});function Dn(e,t){if(t){var n=e.firstChild;if(n&&n===e.lastChild&&n.nodeType===3){n.nodeValue=t;return}}e.textContent=t}i(Dn,"Rb");function An(e,t){var n={};return n[e.toLowerCase()]=t.toLowerCase(),n["Webkit"+e]="webkit"+t,n["Moz"+e]="moz"+t,n}i(An,"Sb");var fn={animationend:An("Animation","AnimationEnd"),animationiteration:An("Animation","AnimationIteration"),animationstart:An("Animation","AnimationStart"),transitionend:An("Transition","TransitionEnd")},yi={},Jr={};B&&(Jr=document.createElement("div").style,"AnimationEvent"in window||(delete fn.animationend.animation,delete fn.animationiteration.animation,delete fn.animationstart.animation),"TransitionEvent"in window||delete fn.transitionend.transition);function eo(e){if(yi[e])return yi[e];if(!fn[e])return e;var t=fn[e],n;for(n in t)if(t.hasOwnProperty(n)&&n in Jr)return yi[e]=t[n];return e}i(eo,"Wb");var to=eo("animationend"),Ci=eo("animationiteration"),wi=eo("animationstart"),Ke=eo("transitionend"),vr="abort canplay canplaythrough durationchange emptied encrypted ended error loadeddata loadedmetadata loadstart pause play playing progress ratechange seeked seeking stalled suspend timeupdate volumechange waiting".split(" "),Wl=new(typeof WeakMap=="function"?WeakMap:Map);function xi(e){var t=Wl.get(e);return t===void 0&&(t=new Map,Wl.set(e,t)),t}i(xi,"cc");function mn(e){var t=e,n=e;if(e.alternate)for(;t.return;)t=t.return;else{e=t;do t=e,t.effectTag&1026&&(n=t.return),e=t.return;while(e)}return t.tag===3?n:null}i(mn,"dc");function Ei(e){if(e.tag===13){var t=e.memoizedState;if(t===null&&(e=e.alternate,e!==null&&(t=e.memoizedState)),t!==null)return t.dehydrated}return null}i(Ei,"ec");function ql(e){if(mn(e)!==e)throw Error(v(188))}i(ql,"fc");function ki(e){var t=e.alternate;if(!t){if(t=mn(e),t===null)throw Error(v(188));return t!==e?null:e}for(var n=e,r=t;;){var s=n.return;if(s===null)break;var c=s.alternate;if(c===null){if(r=s.return,r!==null){n=r;continue}break}if(s.child===c.child){for(c=s.child;c;){if(c===n)return ql(s),e;if(c===r)return ql(s),t;c=c.sibling}throw Error(v(188))}if(n.return!==r.return)n=s,r=c;else{for(var m=!1,h=s.child;h;){if(h===n){m=!0,n=s,r=c;break}if(h===r){m=!0,r=s,n=c;break}h=h.sibling}if(!m){for(h=c.child;h;){if(h===n){m=!0,n=c,r=s;break}if(h===r){m=!0,r=c,n=s;break}h=h.sibling}if(!m)throw Error(v(189))}}if(n.alternate!==r)throw Error(v(190))}if(n.tag!==3)throw Error(v(188));return n.stateNode.current===n?e:t}i(ki,"gc");function gr(e){if(e=ki(e),!e)return null;for(var t=e;;){if(t.tag===5||t.tag===6)return t;if(t.child)t.child.return=t,t=t.child;else{if(t===e)break;for(;!t.sibling;){if(!t.return||t.return===e)return null;t=t.return}t.sibling.return=t.return,t=t.sibling}}return null}i(gr,"hc");function pn(e,t){if(t==null)throw Error(v(30));return e==null?t:Array.isArray(e)?Array.isArray(t)?(e.push.apply(e,t),e):(e.push(t),e):Array.isArray(t)?[e].concat(t):[e,t]}i(pn,"ic");function no(e,t,n){Array.isArray(e)?e.forEach(t,n):e&&t.call(n,e)}i(no,"jc");var yr=null;function va(e){if(e){var t=e._dispatchListeners,n=e._dispatchInstances;if(Array.isArray(t))for(var r=0;r<t.length&&!e.isPropagationStopped();r++)P(e,t[r],n[r]);else t&&P(e,t,n);e._dispatchListeners=null,e._dispatchInstances=null,e.isPersistent()||e.constructor.release(e)}}i(va,"lc");function ro(e){if(e!==null&&(yr=pn(yr,e)),e=yr,yr=null,e){if(no(e,va),yr)throw Error(v(95));if(l)throw e=ae,l=!1,ae=null,e}}i(ro,"mc");function Cr(e){return e=e.target||e.srcElement||window,e.correspondingUseElement&&(e=e.correspondingUseElement),e.nodeType===3?e.parentNode:e}i(Cr,"nc");function wr(e){if(!B)return!1;e="on"+e;var t=e in document;return t||(t=document.createElement("div"),t.setAttribute(e,"return;"),t=typeof t[e]=="function"),t}i(wr,"oc");var In=[];function oo(e){e.topLevelType=null,e.nativeEvent=null,e.targetInst=null,e.ancestors.length=0,10>In.length&&In.push(e)}i(oo,"qc");function bi(e,t,n,r){if(In.length){var s=In.pop();return s.topLevelType=e,s.eventSystemFlags=r,s.nativeEvent=t,s.targetInst=n,s}return{topLevelType:e,eventSystemFlags:r,nativeEvent:t,targetInst:n,ancestors:[]}}i(bi,"rc");function io(e){var t=e.targetInst,n=t;do{if(!n){e.ancestors.push(n);break}var r=n;if(r.tag===3)r=r.stateNode.containerInfo;else{for(;r.return;)r=r.return;r=r.tag!==3?null:r.stateNode.containerInfo}if(!r)break;t=n.tag,t!==5&&t!==6||e.ancestors.push(n),n=Wn(r)}while(n);for(n=0;n<e.ancestors.length;n++){t=e.ancestors[n];var s=Cr(e.nativeEvent);r=e.topLevelType;var c=e.nativeEvent,m=e.eventSystemFlags;n===0&&(m|=64);for(var h=null,L=0;L<V.length;L++){var S=V[L];S&&(S=S.extractEvents(r,t,c,s,m))&&(h=pn(h,S))}ro(h)}}i(io,"sc");function lo(e,t,n){if(!n.has(e)){switch(e){case"scroll":vn(t,"scroll",!0);break;case"focus":case"blur":vn(t,"focus",!0),vn(t,"blur",!0),n.set("blur",null),n.set("focus",null);break;case"cancel":case"close":wr(e)&&vn(t,e,!0);break;case"invalid":case"submit":case"reset":break;default:vr.indexOf(e)===-1&&je(e,t)}n.set(e,null)}}i(lo,"uc");var _i,xr,Er,kr=!1,gt=[],Dt=null,At=null,St=null,nn=new Map,Bt=new Map,Hn=[],Fn="mousedown mouseup touchcancel touchend touchstart auxclick dblclick pointercancel pointerdown pointerup dragend dragstart drop compositionend compositionstart keydown keypress keyup input textInput close cancel copy cut paste click change contextmenu reset submit".split(" "),ga="focus blur dragenter dragleave mouseover mouseout pointerover pointerout gotpointercapture lostpointercapture".split(" ");function hn(e,t){var n=xi(t);Fn.forEach(function(r){lo(r,t,n)}),ga.forEach(function(r){lo(r,t,n)})}i(hn,"Jc");function br(e,t,n,r,s){return{blockedOn:e,topLevelType:t,eventSystemFlags:n|32,nativeEvent:s,container:r}}i(br,"Kc");function Zl(e,t){switch(e){case"focus":case"blur":Dt=null;break;case"dragenter":case"dragleave":At=null;break;case"mouseover":case"mouseout":St=null;break;case"pointerover":case"pointerout":nn.delete(t.pointerId);break;case"gotpointercapture":case"lostpointercapture":Bt.delete(t.pointerId)}}i(Zl,"Lc");function _r(e,t,n,r,s,c){return e===null||e.nativeEvent!==c?(e=br(t,n,r,s,c),t!==null&&(t=qn(t),t!==null&&xr(t)),e):(e.eventSystemFlags|=r,e)}i(_r,"Mc");function ya(e,t,n,r,s){switch(t){case"focus":return Dt=_r(Dt,e,t,n,r,s),!0;case"dragenter":return At=_r(At,e,t,n,r,s),!0;case"mouseover":return St=_r(St,e,t,n,r,s),!0;case"pointerover":var c=s.pointerId;return nn.set(c,_r(nn.get(c)||null,e,t,n,r,s)),!0;case"gotpointercapture":return c=s.pointerId,Bt.set(c,_r(Bt.get(c)||null,e,t,n,r,s)),!0}return!1}i(ya,"Oc");function Et(e){var t=Wn(e.target);if(t!==null){var n=mn(t);if(n!==null){if(t=n.tag,t===13){if(t=Ei(n),t!==null){e.blockedOn=t,y.unstable_runWithPriority(e.priority,function(){Er(n)});return}}else if(t===3&&n.stateNode.hydrate){e.blockedOn=n.tag===3?n.stateNode.containerInfo:null;return}}}e.blockedOn=null}i(Et,"Pc");function so(e){if(e.blockedOn!==null)return!1;var t=Sr(e.topLevelType,e.eventSystemFlags,e.container,e.nativeEvent);if(t!==null){var n=qn(t);return n!==null&&xr(n),e.blockedOn=t,!1}return!0}i(so,"Qc");function ao(e,t,n){so(e)&&n.delete(t)}i(ao,"Sc");function yt(){for(kr=!1;0<gt.length;){var e=gt[0];if(e.blockedOn!==null){e=qn(e.blockedOn),e!==null&&_i(e);break}var t=Sr(e.topLevelType,e.eventSystemFlags,e.container,e.nativeEvent);t!==null?e.blockedOn=t:gt.shift()}Dt!==null&&so(Dt)&&(Dt=null),At!==null&&so(At)&&(At=null),St!==null&&so(St)&&(St=null),nn.forEach(ao),Bt.forEach(ao)}i(yt,"Tc");function Xe(e,t){e.blockedOn===t&&(e.blockedOn=null,kr||(kr=!0,y.unstable_scheduleCallback(y.unstable_NormalPriority,yt)))}i(Xe,"Uc");function Li(e){function t(s){return Xe(s,e)}if(i(t,"b"),0<gt.length){Xe(gt[0],e);for(var n=1;n<gt.length;n++){var r=gt[n];r.blockedOn===e&&(r.blockedOn=null)}}for(Dt!==null&&Xe(Dt,e),At!==null&&Xe(At,e),St!==null&&Xe(St,e),nn.forEach(t),Bt.forEach(t),n=0;n<Hn.length;n++)r=Hn[n],r.blockedOn===e&&(r.blockedOn=null);for(;0<Hn.length&&(n=Hn[0],n.blockedOn===null);)Et(n),n.blockedOn===null&&Hn.shift()}i(Li,"Vc");var zn={},Vn=new Map,uo=new Map,Ql=["abort","abort",to,"animationEnd",Ci,"animationIteration",wi,"animationStart","canplay","canPlay","canplaythrough","canPlayThrough","durationchange","durationChange","emptied","emptied","encrypted","encrypted","ended","ended","error","error","gotpointercapture","gotPointerCapture","load","load","loadeddata","loadedData","loadedmetadata","loadedMetadata","loadstart","loadStart","lostpointercapture","lostPointerCapture","playing","playing","progress","progress","seeking","seeking","stalled","stalled","suspend","suspend","timeupdate","timeUpdate",Ke,"transitionEnd","waiting","waiting"];function co(e,t){for(var n=0;n<e.length;n+=2){var r=e[n],s=e[n+1],c="on"+(s[0].toUpperCase()+s.slice(1));c={phasedRegistrationNames:{bubbled:c,captured:c+"Capture"},dependencies:[r],eventPriority:t},uo.set(r,t),Vn.set(r,c),zn[s]=c}}i(co,"ad"),co("blur blur cancel cancel click click close close contextmenu contextMenu copy copy cut cut auxclick auxClick dblclick doubleClick dragend dragEnd dragstart dragStart drop drop focus focus input input invalid invalid keydown keyDown keypress keyPress keyup keyUp mousedown mouseDown mouseup mouseUp paste paste pause pause play play pointercancel pointerCancel pointerdown pointerDown pointerup pointerUp ratechange rateChange reset reset seeked seeked submit submit touchcancel touchCancel touchend touchEnd touchstart touchStart volumechange volumeChange".split(" "),0),co("drag drag dragenter dragEnter dragexit dragExit dragleave dragLeave dragover dragOver mousemove mouseMove mouseout mouseOut mouseover mouseOver pointermove pointerMove pointerout pointerOut pointerover pointerOver scroll scroll toggle toggle touchmove touchMove wheel wheel".split(" "),1),co(Ql,2);for(var fo="change selectionchange textInput compositionstart compositionend compositionupdate".split(" "),mo=0;mo<fo.length;mo++)uo.set(fo[mo],0);var Kl=y.unstable_UserBlockingPriority,Yl=y.unstable_runWithPriority,Lr=!0;function je(e,t){vn(t,e,!1)}i(je,"F");function vn(e,t,n){var r=uo.get(t);switch(r===void 0?2:r){case 0:r=po.bind(null,t,1,e);break;case 1:r=Si.bind(null,t,1,e);break;default:r=ho.bind(null,t,1,e)}n?e.addEventListener(t,r,!0):e.addEventListener(t,r,!1)}i(vn,"vc");function po(e,t,n,r){Fe||st();var s=ho,c=Fe;Fe=!0;try{nt(s,e,t,n,r)}finally{(Fe=c)||U()}}i(po,"gd");function Si(e,t,n,r){Yl(Kl,ho.bind(null,e,t,n,r))}i(Si,"hd");function ho(e,t,n,r){if(Lr)if(0<gt.length&&-1<Fn.indexOf(e))e=br(null,e,t,n,r),gt.push(e);else{var s=Sr(e,t,n,r);if(s===null)Zl(e,r);else if(-1<Fn.indexOf(e))e=br(s,e,t,n,r),gt.push(e);else if(!ya(s,e,t,n,r)){Zl(e,r),e=bi(e,r,null,t);try{te(io,e)}finally{oo(e)}}}}i(ho,"id");function Sr(e,t,n,r){if(n=Cr(r),n=Wn(n),n!==null){var s=mn(n);if(s===null)n=null;else{var c=s.tag;if(c===13){if(n=Ei(s),n!==null)return n;n=null}else if(c===3){if(s.stateNode.hydrate)return s.tag===3?s.stateNode.containerInfo:null;n=null}else s!==n&&(n=null)}}e=bi(e,r,n,t);try{te(io,e)}finally{oo(e)}return null}i(Sr,"Rc");var $n={animationIterationCount:!0,borderImageOutset:!0,borderImageSlice:!0,borderImageWidth:!0,boxFlex:!0,boxFlexGroup:!0,boxOrdinalGroup:!0,columnCount:!0,columns:!0,flex:!0,flexGrow:!0,flexPositive:!0,flexShrink:!0,flexNegative:!0,flexOrder:!0,gridArea:!0,gridRow:!0,gridRowEnd:!0,gridRowSpan:!0,gridRowStart:!0,gridColumn:!0,gridColumnEnd:!0,gridColumnSpan:!0,gridColumnStart:!0,fontWeight:!0,lineClamp:!0,lineHeight:!0,opacity:!0,order:!0,orphans:!0,tabSize:!0,widows:!0,zIndex:!0,zoom:!0,fillOpacity:!0,floodOpacity:!0,stopOpacity:!0,strokeDasharray:!0,strokeDashoffset:!0,strokeMiterlimit:!0,strokeOpacity:!0,strokeWidth:!0},Ti=["Webkit","ms","Moz","O"];Object.keys($n).forEach(function(e){Ti.forEach(function(t){t=t+e.charAt(0).toUpperCase()+e.substring(1),$n[t]=$n[e]})});function vo(e,t,n){return t==null||typeof t=="boolean"||t===""?"":n||typeof t!="number"||t===0||$n.hasOwnProperty(e)&&$n[e]?(""+t).trim():t+"px"}i(vo,"ld");function Ni(e,t){e=e.style;for(var n in t)if(t.hasOwnProperty(n)){var r=n.indexOf("--")===0,s=vo(n,t[n],r);n==="float"&&(n="cssFloat"),r?e.setProperty(n,s):e[n]=s}}i(Ni,"md");var Gl=I({menuitem:!0},{area:!0,base:!0,br:!0,col:!0,embed:!0,hr:!0,img:!0,input:!0,keygen:!0,link:!0,meta:!0,param:!0,source:!0,track:!0,wbr:!0});function go(e,t){if(t){if(Gl[e]&&(t.children!=null||t.dangerouslySetInnerHTML!=null))throw Error(v(137,e,""));if(t.dangerouslySetInnerHTML!=null){if(t.children!=null)throw Error(v(60));if(!(typeof t.dangerouslySetInnerHTML=="object"&&"__html"in t.dangerouslySetInnerHTML))throw Error(v(61))}if(t.style!=null&&typeof t.style!="object")throw Error(v(62,""))}}i(go,"od");function yo(e,t){if(e.indexOf("-")===-1)return typeof t.is=="string";switch(e){case"annotation-xml":case"color-profile":case"font-face":case"font-face-src":case"font-face-uri":case"font-face-format":case"font-face-name":case"missing-glyph":return!1;default:return!0}}i(yo,"pd");var Mi=hi.html;function It(e,t){e=e.nodeType===9||e.nodeType===11?e:e.ownerDocument;var n=xi(e);t=ie[t];for(var r=0;r<t.length;r++)lo(t[r],e,n)}i(It,"rd");function Tr(){}i(Tr,"sd");function Co(e){if(e=e||(typeof document!="undefined"?document:void 0),typeof e=="undefined")return null;try{return e.activeElement||e.body}catch{return e.body}}i(Co,"td");function Ri(e){for(;e&&e.firstChild;)e=e.firstChild;return e}i(Ri,"ud");function wo(e,t){var n=Ri(e);e=0;for(var r;n;){if(n.nodeType===3){if(r=e+n.textContent.length,e<=t&&r>=t)return{node:n,offset:t-e};e=r}e:{for(;n;){if(n.nextSibling){n=n.nextSibling;break e}n=n.parentNode}n=void 0}n=Ri(n)}}i(wo,"vd");function Pi(e,t){return e&&t?e===t?!0:e&&e.nodeType===3?!1:t&&t.nodeType===3?Pi(e,t.parentNode):"contains"in e?e.contains(t):e.compareDocumentPosition?!!(e.compareDocumentPosition(t)&16):!1:!1}i(Pi,"wd");function Oi(){for(var e=window,t=Co();t instanceof e.HTMLIFrameElement;){try{var n=typeof t.contentWindow.location.href=="string"}catch{n=!1}if(n)e=t.contentWindow;else break;t=Co(e.document)}return t}i(Oi,"xd");function xo(e){var t=e&&e.nodeName&&e.nodeName.toLowerCase();return t&&(t==="input"&&(e.type==="text"||e.type==="search"||e.type==="tel"||e.type==="url"||e.type==="password")||t==="textarea"||e.contentEditable==="true")}i(xo,"yd");var Di="$",jn="/$",Eo="$?",ko="$!",bo=null,_o=null;function Ai(e,t){switch(e){case"button":case"input":case"select":case"textarea":return!!t.autoFocus}return!1}i(Ai,"Fd");function Je(e,t){return e==="textarea"||e==="option"||e==="noscript"||typeof t.children=="string"||typeof t.children=="number"||typeof t.dangerouslySetInnerHTML=="object"&&t.dangerouslySetInnerHTML!==null&&t.dangerouslySetInnerHTML.__html!=null}i(Je,"Gd");var Bn=typeof setTimeout=="function"?setTimeout:void 0,Xl=typeof clearTimeout=="function"?clearTimeout:void 0;function gn(e){for(;e!=null;e=e.nextSibling){var t=e.nodeType;if(t===1||t===3)break}return e}i(gn,"Jd");function Ii(e){e=e.previousSibling;for(var t=0;e;){if(e.nodeType===8){var n=e.data;if(n===Di||n===ko||n===Eo){if(t===0)return e;t--}else n===jn&&t++}e=e.previousSibling}return null}i(Ii,"Kd");var Lo=Math.random().toString(36).slice(2),Ut="__reactInternalInstance$"+Lo,Nr="__reactEventHandlers$"+Lo,Un="__reactContainere$"+Lo;function Wn(e){var t=e[Ut];if(t)return t;for(var n=e.parentNode;n;){if(t=n[Un]||n[Ut]){if(n=t.alternate,t.child!==null||n!==null&&n.child!==null)for(e=Ii(e);e!==null;){if(n=e[Ut])return n;e=Ii(e)}return t}e=n,n=e.parentNode}return null}i(Wn,"tc");function qn(e){return e=e[Ut]||e[Un],!e||e.tag!==5&&e.tag!==6&&e.tag!==13&&e.tag!==3?null:e}i(qn,"Nc");function rn(e){if(e.tag===5||e.tag===6)return e.stateNode;throw Error(v(33))}i(rn,"Pd");function So(e){return e[Nr]||null}i(So,"Qd");function Ht(e){do e=e.return;while(e&&e.tag!==5);return e||null}i(Ht,"Rd");function Hi(e,t){var n=e.stateNode;if(!n)return null;var r=$(n);if(!r)return null;n=r[t];e:switch(t){case"onClick":case"onClickCapture":case"onDoubleClick":case"onDoubleClickCapture":case"onMouseDown":case"onMouseDownCapture":case"onMouseMove":case"onMouseMoveCapture":case"onMouseUp":case"onMouseUpCapture":case"onMouseEnter":(r=!r.disabled)||(e=e.type,r=!(e==="button"||e==="input"||e==="select"||e==="textarea")),e=!r;break e;default:e=!1}if(e)return null;if(n&&typeof n!="function")throw Error(v(231,t,typeof n));return n}i(Hi,"Sd");function Fi(e,t,n){(t=Hi(e,n.dispatchConfig.phasedRegistrationNames[t]))&&(n._dispatchListeners=pn(n._dispatchListeners,t),n._dispatchInstances=pn(n._dispatchInstances,e))}i(Fi,"Td");function Ca(e){if(e&&e.dispatchConfig.phasedRegistrationNames){for(var t=e._targetInst,n=[];t;)n.push(t),t=Ht(t);for(t=n.length;0<t--;)Fi(n[t],"captured",e);for(t=0;t<n.length;t++)Fi(n[t],"bubbled",e)}}i(Ca,"Ud");function To(e,t,n){e&&n&&n.dispatchConfig.registrationName&&(t=Hi(e,n.dispatchConfig.registrationName))&&(n._dispatchListeners=pn(n._dispatchListeners,t),n._dispatchInstances=pn(n._dispatchInstances,e))}i(To,"Vd");function Jl(e){e&&e.dispatchConfig.registrationName&&To(e._targetInst,null,e)}i(Jl,"Wd");function yn(e){no(e,Ca)}i(yn,"Xd");var Wt=null,No=null,Mo=null;function zi(){if(Mo)return Mo;var e,t=No,n=t.length,r,s="value"in Wt?Wt.value:Wt.textContent,c=s.length;for(e=0;e<n&&t[e]===s[e];e++);var m=n-e;for(r=1;r<=m&&t[n-r]===s[c-r];r++);return Mo=s.slice(e,1<r?1-r:void 0)}i(zi,"ae");function Mr(){return!0}i(Mr,"be");function Rr(){return!1}i(Rr,"ce");function vt(e,t,n,r){this.dispatchConfig=e,this._targetInst=t,this.nativeEvent=n,e=this.constructor.Interface;for(var s in e)e.hasOwnProperty(s)&&((t=e[s])?this[s]=t(n):s==="target"?this.target=r:this[s]=n[s]);return this.isDefaultPrevented=(n.defaultPrevented!=null?n.defaultPrevented:n.returnValue===!1)?Mr:Rr,this.isPropagationStopped=Rr,this}i(vt,"G"),I(vt.prototype,{preventDefault:i(function(){this.defaultPrevented=!0;var e=this.nativeEvent;e&&(e.preventDefault?e.preventDefault():typeof e.returnValue!="unknown"&&(e.returnValue=!1),this.isDefaultPrevented=Mr)},"preventDefault"),stopPropagation:i(function(){var e=this.nativeEvent;e&&(e.stopPropagation?e.stopPropagation():typeof e.cancelBubble!="unknown"&&(e.cancelBubble=!0),this.isPropagationStopped=Mr)},"stopPropagation"),persist:i(function(){this.isPersistent=Mr},"persist"),isPersistent:Rr,destructor:i(function(){var e=this.constructor.Interface,t;for(t in e)this[t]=null;this.nativeEvent=this._targetInst=this.dispatchConfig=null,this.isPropagationStopped=this.isDefaultPrevented=Rr,this._dispatchInstances=this._dispatchListeners=null},"destructor")}),vt.Interface={type:null,target:null,currentTarget:i(function(){return null},"currentTarget"),eventPhase:null,bubbles:null,cancelable:null,timeStamp:i(function(e){return e.timeStamp||Date.now()},"timeStamp"),defaultPrevented:null,isTrusted:null},vt.extend=function(e){function t(){}i(t,"b");function n(){return r.apply(this,arguments)}i(n,"c");var r=this;t.prototype=r.prototype;var s=new t;return I(s,n.prototype),n.prototype=s,n.prototype.constructor=n,n.Interface=I({},r.Interface,e),n.extend=r.extend,Ro(n),n},Ro(vt);function es(e,t,n,r){if(this.eventPool.length){var s=this.eventPool.pop();return this.call(s,e,t,n,r),s}return new this(e,t,n,r)}i(es,"ee");function Pr(e){if(!(e instanceof this))throw Error(v(279));e.destructor(),10>this.eventPool.length&&this.eventPool.push(e)}i(Pr,"fe");function Ro(e){e.eventPool=[],e.getPooled=es,e.release=Pr}i(Ro,"de");var ts=vt.extend({data:null}),ns=vt.extend({data:null}),Vi=[9,13,27,32],Po=B&&"CompositionEvent"in window,Zn=null;B&&"documentMode"in document&&(Zn=document.documentMode);var rs=B&&"TextEvent"in window&&!Zn,os=B&&(!Po||Zn&&8<Zn&&11>=Zn),Oo=" ",Ft={beforeInput:{phasedRegistrationNames:{bubbled:"onBeforeInput",captured:"onBeforeInputCapture"},dependencies:["compositionend","keypress","textInput","paste"]},compositionEnd:{phasedRegistrationNames:{bubbled:"onCompositionEnd",captured:"onCompositionEndCapture"},dependencies:"blur compositionend keydown keypress keyup mousedown".split(" ")},compositionStart:{phasedRegistrationNames:{bubbled:"onCompositionStart",captured:"onCompositionStartCapture"},dependencies:"blur compositionstart keydown keypress keyup mousedown".split(" ")},compositionUpdate:{phasedRegistrationNames:{bubbled:"onCompositionUpdate",captured:"onCompositionUpdateCapture"},dependencies:"blur compositionupdate keydown keypress keyup mousedown".split(" ")}},$i=!1;function ji(e,t){switch(e){case"keyup":return Vi.indexOf(t.keyCode)!==-1;case"keydown":return t.keyCode!==229;case"keypress":case"mousedown":case"blur":return!0;default:return!1}}i(ji,"qe");function Do(e){return e=e.detail,typeof e=="object"&&"data"in e?e.data:null}i(Do,"re");var on=!1;function is(e,t){switch(e){case"compositionend":return Do(t);case"keypress":return t.which!==32?null:($i=!0,Oo);case"textInput":return e=t.data,e===Oo&&$i?null:e;default:return null}}i(is,"te");function ls(e,t){if(on)return e==="compositionend"||!Po&&ji(e,t)?(e=zi(),Mo=No=Wt=null,on=!1,e):null;switch(e){case"paste":return null;case"keypress":if(!(t.ctrlKey||t.altKey||t.metaKey)||t.ctrlKey&&t.altKey){if(t.char&&1<t.char.length)return t.char;if(t.which)return String.fromCharCode(t.which)}return null;case"compositionend":return os&&t.locale!=="ko"?null:t.data;default:return null}}i(ls,"ue");var ss={eventTypes:Ft,extractEvents:i(function(e,t,n,r){var s;if(Po)e:{switch(e){case"compositionstart":var c=Ft.compositionStart;break e;case"compositionend":c=Ft.compositionEnd;break e;case"compositionupdate":c=Ft.compositionUpdate;break e}c=void 0}else on?ji(e,n)&&(c=Ft.compositionEnd):e==="keydown"&&n.keyCode===229&&(c=Ft.compositionStart);return c?(os&&n.locale!=="ko"&&(on||c!==Ft.compositionStart?c===Ft.compositionEnd&&on&&(s=zi()):(Wt=r,No="value"in Wt?Wt.value:Wt.textContent,on=!0)),c=ts.getPooled(c,t,n,r),s?c.data=s:(s=Do(n),s!==null&&(c.data=s)),yn(c),s=c):s=null,(e=rs?is(e,n):ls(e,n))?(t=ns.getPooled(Ft.beforeInput,t,n,r),t.data=e,yn(t)):t=null,s===null?t:t===null?s:[s,t]},"extractEvents")},Bi={color:!0,date:!0,datetime:!0,"datetime-local":!0,email:!0,month:!0,number:!0,password:!0,range:!0,search:!0,tel:!0,text:!0,time:!0,url:!0,week:!0};function Qn(e){var t=e&&e.nodeName&&e.nodeName.toLowerCase();return t==="input"?!!Bi[e.type]:t==="textarea"}i(Qn,"xe");var Ui={change:{phasedRegistrationNames:{bubbled:"onChange",captured:"onChangeCapture"},dependencies:"blur change click focus input keydown keyup selectionchange".split(" ")}};function Or(e,t,n){return e=vt.getPooled(Ui.change,e,t,n),e.type="change",Te(n),yn(e),e}i(Or,"ze");var Cn=null,Kn=null;function as(e){ro(e)}i(as,"Ce");function Yn(e){var t=rn(e);if(jl(t))return e}i(Yn,"De");function us(e,t){if(e==="change")return t}i(us,"Ee");var Wi=!1;B&&(Wi=wr("input")&&(!document.documentMode||9<document.documentMode));function cs(){Cn&&(Cn.detachEvent("onpropertychange",qi),Kn=Cn=null)}i(cs,"Ge");function qi(e){if(e.propertyName==="value"&&Yn(Kn))if(e=Or(Kn,e,Cr(e)),Fe)ro(e);else{Fe=!0;try{Qe(as,e)}finally{Fe=!1,U()}}}i(qi,"He");function wa(e,t,n){e==="focus"?(cs(),Cn=t,Kn=n,Cn.attachEvent("onpropertychange",qi)):e==="blur"&&cs()}i(wa,"Ie");function ds(e){if(e==="selectionchange"||e==="keyup"||e==="keydown")return Yn(Kn)}i(ds,"Je");function fs(e,t){if(e==="click")return Yn(t)}i(fs,"Ke");function ms(e,t){if(e==="input"||e==="change")return Yn(t)}i(ms,"Le");var ps={eventTypes:Ui,_isInputEventSupported:Wi,extractEvents:i(function(e,t,n,r){var s=t?rn(t):window,c=s.nodeName&&s.nodeName.toLowerCase();if(c==="select"||c==="input"&&s.type==="file")var m=us;else if(Qn(s))if(Wi)m=ms;else{m=ds;var h=wa}else(c=s.nodeName)&&c.toLowerCase()==="input"&&(s.type==="checkbox"||s.type==="radio")&&(m=fs);if(m&&(m=m(e,t)))return Or(m,n,r);h&&h(e,s,t),e==="blur"&&(e=s._wrapperState)&&e.controlled&&s.type==="number"&&Kr(s,"number",s.value)},"extractEvents")},wn=vt.extend({view:null,detail:null}),hs={Alt:"altKey",Control:"ctrlKey",Meta:"metaKey",Shift:"shiftKey"};function Zi(e){var t=this.nativeEvent;return t.getModifierState?t.getModifierState(e):(e=hs[e])?!!t[e]:!1}i(Zi,"Pe");function Ao(){return Zi}i(Ao,"Qe");var vs=0,Qi=0,Ki=!1,Yi=!1,Gn=wn.extend({screenX:null,screenY:null,clientX:null,clientY:null,pageX:null,pageY:null,ctrlKey:null,shiftKey:null,altKey:null,metaKey:null,getModifierState:Ao,button:null,buttons:null,relatedTarget:i(function(e){return e.relatedTarget||(e.fromElement===e.srcElement?e.toElement:e.fromElement)},"relatedTarget"),movementX:i(function(e){if("movementX"in e)return e.movementX;var t=vs;return vs=e.screenX,Ki?e.type==="mousemove"?e.screenX-t:0:(Ki=!0,0)},"movementX"),movementY:i(function(e){if("movementY"in e)return e.movementY;var t=Qi;return Qi=e.screenY,Yi?e.type==="mousemove"?e.screenY-t:0:(Yi=!0,0)},"movementY")}),Gi=Gn.extend({pointerId:null,width:null,height:null,pressure:null,tangentialPressure:null,tiltX:null,tiltY:null,twist:null,pointerType:null,isPrimary:null}),Xn={mouseEnter:{registrationName:"onMouseEnter",dependencies:["mouseout","mouseover"]},mouseLeave:{registrationName:"onMouseLeave",dependencies:["mouseout","mouseover"]},pointerEnter:{registrationName:"onPointerEnter",dependencies:["pointerout","pointerover"]},pointerLeave:{registrationName:"onPointerLeave",dependencies:["pointerout","pointerover"]}},gs={eventTypes:Xn,extractEvents:i(function(e,t,n,r,s){var c=e==="mouseover"||e==="pointerover",m=e==="mouseout"||e==="pointerout";if(c&&!(s&32)&&(n.relatedTarget||n.fromElement)||!m&&!c)return null;if(c=r.window===r?r:(c=r.ownerDocument)?c.defaultView||c.parentWindow:window,m){if(m=t,t=(t=n.relatedTarget||n.toElement)?Wn(t):null,t!==null){var h=mn(t);(t!==h||t.tag!==5&&t.tag!==6)&&(t=null)}}else m=null;if(m===t)return null;if(e==="mouseout"||e==="mouseover")var L=Gn,S=Xn.mouseLeave,re=Xn.mouseEnter,ue="mouse";else(e==="pointerout"||e==="pointerover")&&(L=Gi,S=Xn.pointerLeave,re=Xn.pointerEnter,ue="pointer");if(e=m==null?c:rn(m),c=t==null?c:rn(t),S=L.getPooled(S,m,n,r),S.type=ue+"leave",S.target=e,S.relatedTarget=c,n=L.getPooled(re,t,n,r),n.type=ue+"enter",n.target=c,n.relatedTarget=e,r=m,ue=t,r&&ue)e:{for(L=r,re=ue,m=0,e=L;e;e=Ht(e))m++;for(e=0,t=re;t;t=Ht(t))e++;for(;0<m-e;)L=Ht(L),m--;for(;0<e-m;)re=Ht(re),e--;for(;m--;){if(L===re||L===re.alternate)break e;L=Ht(L),re=Ht(re)}L=null}else L=null;for(re=L,L=[];r&&r!==re&&(m=r.alternate,!(m!==null&&m===re));)L.push(r),r=Ht(r);for(r=[];ue&&ue!==re&&(m=ue.alternate,!(m!==null&&m===re));)r.push(ue),ue=Ht(ue);for(ue=0;ue<L.length;ue++)To(L[ue],"bubbled",S);for(ue=r.length;0<ue--;)To(r[ue],"captured",n);return s&64?[S,n]:[S]},"extractEvents")};function ys(e,t){return e===t&&(e!==0||1/e===1/t)||e!==e&&t!==t}i(ys,"Ze");var ln=typeof Object.is=="function"?Object.is:ys,Cs=Object.prototype.hasOwnProperty;function Jn(e,t){if(ln(e,t))return!0;if(typeof e!="object"||e===null||typeof t!="object"||t===null)return!1;var n=Object.keys(e),r=Object.keys(t);if(n.length!==r.length)return!1;for(r=0;r<n.length;r++)if(!Cs.call(t,n[r])||!ln(e[n[r]],t[n[r]]))return!1;return!0}i(Jn,"bf");var Xi=B&&"documentMode"in document&&11>=document.documentMode,Io={select:{phasedRegistrationNames:{bubbled:"onSelect",captured:"onSelectCapture"},dependencies:"blur contextmenu dragend focus keydown keyup mousedown mouseup selectionchange".split(" ")}},xn=null,Ho=null,er=null,Fo=!1;function Ji(e,t){var n=t.window===t?t.document:t.nodeType===9?t:t.ownerDocument;return Fo||xn==null||xn!==Co(n)?null:(n=xn,"selectionStart"in n&&xo(n)?n={start:n.selectionStart,end:n.selectionEnd}:(n=(n.ownerDocument&&n.ownerDocument.defaultView||window).getSelection(),n={anchorNode:n.anchorNode,anchorOffset:n.anchorOffset,focusNode:n.focusNode,focusOffset:n.focusOffset}),er&&Jn(er,n)?null:(er=n,e=vt.getPooled(Io.select,Ho,e,t),e.type="select",e.target=xn,yn(e),e))}i(Ji,"jf");var ws={eventTypes:Io,extractEvents:i(function(e,t,n,r,s,c){if(s=c||(r.window===r?r.document:r.nodeType===9?r:r.ownerDocument),!(c=!s)){e:{s=xi(s),c=ie.onSelect;for(var m=0;m<c.length;m++)if(!s.has(c[m])){s=!1;break e}s=!0}c=!s}if(c)return null;switch(s=t?rn(t):window,e){case"focus":(Qn(s)||s.contentEditable==="true")&&(xn=s,Ho=t,er=null);break;case"blur":er=Ho=xn=null;break;case"mousedown":Fo=!0;break;case"contextmenu":case"mouseup":case"dragend":return Fo=!1,Ji(n,r);case"selectionchange":if(Xi)break;case"keydown":case"keyup":return Ji(n,r)}return null},"extractEvents")},xs=vt.extend({animationName:null,elapsedTime:null,pseudoElement:null}),el=vt.extend({clipboardData:i(function(e){return"clipboardData"in e?e.clipboardData:window.clipboardData},"clipboardData")}),Es=wn.extend({relatedTarget:null});function Dr(e){var t=e.keyCode;return"charCode"in e?(e=e.charCode,e===0&&t===13&&(e=13)):e=t,e===10&&(e=13),32<=e||e===13?e:0}i(Dr,"of");var zo={Esc:"Escape",Spacebar:" ",Left:"ArrowLeft",Up:"ArrowUp",Right:"ArrowRight",Down:"ArrowDown",Del:"Delete",Win:"OS",Menu:"ContextMenu",Apps:"ContextMenu",Scroll:"ScrollLock",MozPrintableKey:"Unidentified"},En={8:"Backspace",9:"Tab",12:"Clear",13:"Enter",16:"Shift",17:"Control",18:"Alt",19:"Pause",20:"CapsLock",27:"Escape",32:" ",33:"PageUp",34:"PageDown",35:"End",36:"Home",37:"ArrowLeft",38:"ArrowUp",39:"ArrowRight",40:"ArrowDown",45:"Insert",46:"Delete",112:"F1",113:"F2",114:"F3",115:"F4",116:"F5",117:"F6",118:"F7",119:"F8",120:"F9",121:"F10",122:"F11",123:"F12",144:"NumLock",145:"ScrollLock",224:"Meta"},Vo=wn.extend({key:i(function(e){if(e.key){var t=zo[e.key]||e.key;if(t!=="Unidentified")return t}return e.type==="keypress"?(e=Dr(e),e===13?"Enter":String.fromCharCode(e)):e.type==="keydown"||e.type==="keyup"?En[e.keyCode]||"Unidentified":""},"key"),location:null,ctrlKey:null,shiftKey:null,altKey:null,metaKey:null,repeat:null,locale:null,getModifierState:Ao,charCode:i(function(e){return e.type==="keypress"?Dr(e):0},"charCode"),keyCode:i(function(e){return e.type==="keydown"||e.type==="keyup"?e.keyCode:0},"keyCode"),which:i(function(e){return e.type==="keypress"?Dr(e):e.type==="keydown"||e.type==="keyup"?e.keyCode:0},"which")}),$o=Gn.extend({dataTransfer:null}),jo=wn.extend({touches:null,targetTouches:null,changedTouches:null,altKey:null,metaKey:null,ctrlKey:null,shiftKey:null,getModifierState:Ao}),Bo=vt.extend({propertyName:null,elapsedTime:null,pseudoElement:null}),Uo=Gn.extend({deltaX:i(function(e){return"deltaX"in e?e.deltaX:"wheelDeltaX"in e?-e.wheelDeltaX:0},"deltaX"),deltaY:i(function(e){return"deltaY"in e?e.deltaY:"wheelDeltaY"in e?-e.wheelDeltaY:"wheelDelta"in e?-e.wheelDelta:0},"deltaY"),deltaZ:null,deltaMode:null}),Wo={eventTypes:zn,extractEvents:i(function(e,t,n,r){var s=Vn.get(e);if(!s)return null;switch(e){case"keypress":if(Dr(n)===0)return null;case"keydown":case"keyup":e=Vo;break;case"blur":case"focus":e=Es;break;case"click":if(n.button===2)return null;case"auxclick":case"dblclick":case"mousedown":case"mousemove":case"mouseup":case"mouseout":case"mouseover":case"contextmenu":e=Gn;break;case"drag":case"dragend":case"dragenter":case"dragexit":case"dragleave":case"dragover":case"dragstart":case"drop":e=$o;break;case"touchcancel":case"touchend":case"touchmove":case"touchstart":e=jo;break;case to:case Ci:case wi:e=xs;break;case Ke:e=Bo;break;case"scroll":e=wn;break;case"wheel":e=Uo;break;case"copy":case"cut":case"paste":e=el;break;case"gotpointercapture":case"lostpointercapture":case"pointercancel":case"pointerdown":case"pointermove":case"pointerout":case"pointerover":case"pointerup":e=Gi;break;default:e=vt}return t=e.getPooled(s,t,n,r),yn(t),t},"extractEvents")};if(_)throw Error(v(101));_=Array.prototype.slice.call("ResponderEventPlugin SimpleEventPlugin EnterLeaveEventPlugin ChangeEventPlugin SelectEventPlugin BeforeInputEventPlugin".split(" ")),q();var qo=qn;$=So,Z=qo,me=rn,Q({SimpleEventPlugin:Wo,EnterLeaveEventPlugin:gs,ChangeEventPlugin:ps,SelectEventPlugin:ws,BeforeInputEventPlugin:ss});var zt=[],qt=-1;function Ve(e){0>qt||(e.current=zt[qt],zt[qt]=null,qt--)}i(Ve,"H");function We(e,t){qt++,zt[qt]=e.current,e.current=t}i(We,"I");var Tt={},o={current:Tt},a={current:!1},u=Tt;function d(e,t){var n=e.type.contextTypes;if(!n)return Tt;var r=e.stateNode;if(r&&r.__reactInternalMemoizedUnmaskedChildContext===t)return r.__reactInternalMemoizedMaskedChildContext;var s={},c;for(c in n)s[c]=t[c];return r&&(e=e.stateNode,e.__reactInternalMemoizedUnmaskedChildContext=t,e.__reactInternalMemoizedMaskedChildContext=s),s}i(d,"Cf");function f(e){return e=e.childContextTypes,e!=null}i(f,"L");function p(){Ve(a),Ve(o)}i(p,"Df");function g(e,t,n){if(o.current!==Tt)throw Error(v(168));We(o,t),We(a,n)}i(g,"Ef");function C(e,t,n){var r=e.stateNode;if(e=t.childContextTypes,typeof r.getChildContext!="function")return n;r=r.getChildContext();for(var s in r)if(!(s in e))throw Error(v(108,jt(t)||"Unknown",s));return I({},n,{},r)}i(C,"Ff");function k(e){return e=(e=e.stateNode)&&e.__reactInternalMemoizedMergedChildContext||Tt,u=o.current,We(o,e),We(a,a.current),!0}i(k,"Gf");function D(e,t,n){var r=e.stateNode;if(!r)throw Error(v(169));n?(e=C(e,t,u),r.__reactInternalMemoizedMergedChildContext=e,Ve(a),Ve(o),We(o,e)):Ve(a),We(a,n)}i(D,"Hf");var K=y.unstable_runWithPriority,Y=y.unstable_scheduleCallback,ne=y.unstable_cancelCallback,Ie=y.unstable_requestPaint,$e=y.unstable_now,Be=y.unstable_getCurrentPriorityLevel,be=y.unstable_ImmediatePriority,Re=y.unstable_UserBlockingPriority,He=y.unstable_NormalPriority,et=y.unstable_LowPriority,at=y.unstable_IdlePriority,tt={},Ne=y.unstable_shouldYield,Zt=Ie!==void 0?Ie:function(){},qe=null,ze=null,ut=!1,Zo=$e(),Ct=1e4>Zo?$e:function(){return $e()-Zo};function tl(){switch(Be()){case be:return 99;case Re:return 98;case He:return 97;case et:return 96;case at:return 95;default:throw Error(v(332))}}i(tl,"ag");function xa(e){switch(e){case 99:return be;case 98:return Re;case 97:return He;case 96:return et;case 95:return at;default:throw Error(v(332))}}i(xa,"bg");function kn(e,t){return e=xa(e),K(e,t)}i(kn,"cg");function Ea(e,t,n){return e=xa(e),Y(e,t,n)}i(Ea,"dg");function ka(e){return qe===null?(qe=[e],ze=Y(be,ba)):qe.push(e),tt}i(ka,"eg");function Qt(){if(ze!==null){var e=ze;ze=null,ne(e)}ba()}i(Qt,"gg");function ba(){if(!ut&&qe!==null){ut=!0;var e=0;try{var t=qe;kn(99,function(){for(;e<t.length;e++){var n=t[e];do n=n(!0);while(n!==null)}}),qe=null}catch(n){throw qe!==null&&(qe=qe.slice(e+1)),Y(be,Qt),n}finally{ut=!1}}}i(ba,"fg");function nl(e,t,n){return n/=10,1073741821-(((1073741821-e+t/10)/n|0)+1)*n}i(nl,"hg");function Vt(e,t){if(e&&e.defaultProps){t=I({},t),e=e.defaultProps;for(var n in e)t[n]===void 0&&(t[n]=e[n])}return t}i(Vt,"ig");var rl={current:null},ol=null,Ar=null,il=null;function ks(){il=Ar=ol=null}i(ks,"ng");function bs(e){var t=rl.current;Ve(rl),e.type._context._currentValue=t}i(bs,"og");function _a(e,t){for(;e!==null;){var n=e.alternate;if(e.childExpirationTime<t)e.childExpirationTime=t,n!==null&&n.childExpirationTime<t&&(n.childExpirationTime=t);else if(n!==null&&n.childExpirationTime<t)n.childExpirationTime=t;else break;e=e.return}}i(_a,"pg");function Ir(e,t){ol=e,il=Ar=null,e=e.dependencies,e!==null&&e.firstContext!==null&&(e.expirationTime>=t&&(Yt=!0),e.firstContext=null)}i(Ir,"qg");function Nt(e,t){if(il!==e&&t!==!1&&t!==0)if((typeof t!="number"||t===1073741823)&&(il=e,t=1073741823),t={context:e,observedBits:t,next:null},Ar===null){if(ol===null)throw Error(v(308));Ar=t,ol.dependencies={expirationTime:0,firstContext:t,responders:null}}else Ar=Ar.next=t;return e._currentValue}i(Nt,"sg");var bn=!1;function _s(e){e.updateQueue={baseState:e.memoizedState,baseQueue:null,shared:{pending:null},effects:null}}i(_s,"ug");function Ls(e,t){e=e.updateQueue,t.updateQueue===e&&(t.updateQueue={baseState:e.baseState,baseQueue:e.baseQueue,shared:e.shared,effects:e.effects})}i(Ls,"vg");function _n(e,t){return e={expirationTime:e,suspenseConfig:t,tag:0,payload:null,callback:null,next:null},e.next=e}i(_n,"wg");function Ln(e,t){if(e=e.updateQueue,e!==null){e=e.shared;var n=e.pending;n===null?t.next=t:(t.next=n.next,n.next=t),e.pending=t}}i(Ln,"xg");function La(e,t){var n=e.alternate;n!==null&&Ls(n,e),e=e.updateQueue,n=e.baseQueue,n===null?(e.baseQueue=t.next=t,t.next=t):(t.next=n.next,n.next=t)}i(La,"yg");function Qo(e,t,n,r){var s=e.updateQueue;bn=!1;var c=s.baseQueue,m=s.shared.pending;if(m!==null){if(c!==null){var h=c.next;c.next=m.next,m.next=h}c=m,s.shared.pending=null,h=e.alternate,h!==null&&(h=h.updateQueue,h!==null&&(h.baseQueue=m))}if(c!==null){h=c.next;var L=s.baseState,S=0,re=null,ue=null,Me=null;if(h!==null){var Ae=h;do{if(m=Ae.expirationTime,m<r){var Rt={expirationTime:Ae.expirationTime,suspenseConfig:Ae.suspenseConfig,tag:Ae.tag,payload:Ae.payload,callback:Ae.callback,next:null};Me===null?(ue=Me=Rt,re=L):Me=Me.next=Rt,m>S&&(S=m)}else{Me!==null&&(Me=Me.next={expirationTime:1073741823,suspenseConfig:Ae.suspenseConfig,tag:Ae.tag,payload:Ae.payload,callback:Ae.callback,next:null}),Eu(m,Ae.suspenseConfig);e:{var ct=e,b=Ae;switch(m=t,Rt=n,b.tag){case 1:if(ct=b.payload,typeof ct=="function"){L=ct.call(Rt,L,m);break e}L=ct;break e;case 3:ct.effectTag=ct.effectTag&-4097|64;case 0:if(ct=b.payload,m=typeof ct=="function"?ct.call(Rt,L,m):ct,m==null)break e;L=I({},L,m);break e;case 2:bn=!0}}Ae.callback!==null&&(e.effectTag|=32,m=s.effects,m===null?s.effects=[Ae]:m.push(Ae))}if(Ae=Ae.next,Ae===null||Ae===h){if(m=s.shared.pending,m===null)break;Ae=c.next=m.next,m.next=h,s.baseQueue=c=m,s.shared.pending=null}}while(!0)}Me===null?re=L:Me.next=ue,s.baseState=re,s.baseQueue=Me,Pl(S),e.expirationTime=S,e.memoizedState=L}}i(Qo,"zg");function Sa(e,t,n){if(e=t.effects,t.effects=null,e!==null)for(t=0;t<e.length;t++){var r=e[t],s=r.callback;if(s!==null){if(r.callback=null,r=s,s=n,typeof r!="function")throw Error(v(191,r));r.call(s)}}}i(Sa,"Cg");var Ko=xt.ReactCurrentBatchConfig,Ta=new le.Component().refs;function ll(e,t,n,r){t=e.memoizedState,n=n(r,t),n=n==null?t:I({},t,n),e.memoizedState=n,e.expirationTime===0&&(e.updateQueue.baseState=n)}i(ll,"Fg");var sl={isMounted:i(function(e){return(e=e._reactInternalFiber)?mn(e)===e:!1},"isMounted"),enqueueSetState:i(function(e,t,n){e=e._reactInternalFiber;var r=Xt(),s=Ko.suspense;r=lr(r,e,s),s=_n(r,s),s.payload=t,n!=null&&(s.callback=n),Ln(e,s),Mn(e,r)},"enqueueSetState"),enqueueReplaceState:i(function(e,t,n){e=e._reactInternalFiber;var r=Xt(),s=Ko.suspense;r=lr(r,e,s),s=_n(r,s),s.tag=1,s.payload=t,n!=null&&(s.callback=n),Ln(e,s),Mn(e,r)},"enqueueReplaceState"),enqueueForceUpdate:i(function(e,t){e=e._reactInternalFiber;var n=Xt(),r=Ko.suspense;n=lr(n,e,r),r=_n(n,r),r.tag=2,t!=null&&(r.callback=t),Ln(e,r),Mn(e,n)},"enqueueForceUpdate")};function Na(e,t,n,r,s,c,m){return e=e.stateNode,typeof e.shouldComponentUpdate=="function"?e.shouldComponentUpdate(r,c,m):t.prototype&&t.prototype.isPureReactComponent?!Jn(n,r)||!Jn(s,c):!0}i(Na,"Kg");function Ma(e,t,n){var r=!1,s=Tt,c=t.contextType;return typeof c=="object"&&c!==null?c=Nt(c):(s=f(t)?u:o.current,r=t.contextTypes,c=(r=r!=null)?d(e,s):Tt),t=new t(n,c),e.memoizedState=t.state!==null&&t.state!==void 0?t.state:null,t.updater=sl,e.stateNode=t,t._reactInternalFiber=e,r&&(e=e.stateNode,e.__reactInternalMemoizedUnmaskedChildContext=s,e.__reactInternalMemoizedMaskedChildContext=c),t}i(Ma,"Lg");function Ra(e,t,n,r){e=t.state,typeof t.componentWillReceiveProps=="function"&&t.componentWillReceiveProps(n,r),typeof t.UNSAFE_componentWillReceiveProps=="function"&&t.UNSAFE_componentWillReceiveProps(n,r),t.state!==e&&sl.enqueueReplaceState(t,t.state,null)}i(Ra,"Mg");function Ss(e,t,n,r){var s=e.stateNode;s.props=n,s.state=e.memoizedState,s.refs=Ta,_s(e);var c=t.contextType;typeof c=="object"&&c!==null?s.context=Nt(c):(c=f(t)?u:o.current,s.context=d(e,c)),Qo(e,n,s,r),s.state=e.memoizedState,c=t.getDerivedStateFromProps,typeof c=="function"&&(ll(e,t,c,n),s.state=e.memoizedState),typeof t.getDerivedStateFromProps=="function"||typeof s.getSnapshotBeforeUpdate=="function"||typeof s.UNSAFE_componentWillMount!="function"&&typeof s.componentWillMount!="function"||(t=s.state,typeof s.componentWillMount=="function"&&s.componentWillMount(),typeof s.UNSAFE_componentWillMount=="function"&&s.UNSAFE_componentWillMount(),t!==s.state&&sl.enqueueReplaceState(s,s.state,null),Qo(e,n,s,r),s.state=e.memoizedState),typeof s.componentDidMount=="function"&&(e.effectTag|=4)}i(Ss,"Ng");var al=Array.isArray;function Yo(e,t,n){if(e=n.ref,e!==null&&typeof e!="function"&&typeof e!="object"){if(n._owner){if(n=n._owner,n){if(n.tag!==1)throw Error(v(309));var r=n.stateNode}if(!r)throw Error(v(147,e));var s=""+e;return t!==null&&t.ref!==null&&typeof t.ref=="function"&&t.ref._stringRef===s?t.ref:(t=i(function(c){var m=r.refs;m===Ta&&(m=r.refs={}),c===null?delete m[s]:m[s]=c},"b"),t._stringRef=s,t)}if(typeof e!="string")throw Error(v(284));if(!n._owner)throw Error(v(290,e))}return e}i(Yo,"Pg");function ul(e,t){if(e.type!=="textarea")throw Error(v(31,Object.prototype.toString.call(t)==="[object Object]"?"object with keys {"+Object.keys(t).join(", ")+"}":t,""))}i(ul,"Qg");function Pa(e){function t(b,x){if(e){var N=b.lastEffect;N!==null?(N.nextEffect=x,b.lastEffect=x):b.firstEffect=b.lastEffect=x,x.nextEffect=null,x.effectTag=8}}i(t,"b");function n(b,x){if(!e)return null;for(;x!==null;)t(b,x),x=x.sibling;return null}i(n,"c");function r(b,x){for(b=new Map;x!==null;)x.key!==null?b.set(x.key,x):b.set(x.index,x),x=x.sibling;return b}i(r,"d");function s(b,x){return b=cr(b,x),b.index=0,b.sibling=null,b}i(s,"e");function c(b,x,N){return b.index=N,e?(N=b.alternate,N!==null?(N=N.index,N<x?(b.effectTag=2,x):N):(b.effectTag=2,x)):x}i(c,"f");function m(b){return e&&b.alternate===null&&(b.effectTag=2),b}i(m,"g");function h(b,x,N,j){return x===null||x.tag!==6?(x=aa(N,b.mode,j),x.return=b,x):(x=s(x,N),x.return=b,x)}i(h,"h");function L(b,x,N,j){return x!==null&&x.elementType===N.type?(j=s(x,N.props),j.ref=Yo(b,x,N),j.return=b,j):(j=Ol(N.type,N.key,N.props,null,b.mode,j),j.ref=Yo(b,x,N),j.return=b,j)}i(L,"k");function S(b,x,N,j){return x===null||x.tag!==4||x.stateNode.containerInfo!==N.containerInfo||x.stateNode.implementation!==N.implementation?(x=ua(N,b.mode,j),x.return=b,x):(x=s(x,N.children||[]),x.return=b,x)}i(S,"l");function re(b,x,N,j,X){return x===null||x.tag!==7?(x=Rn(N,b.mode,j,X),x.return=b,x):(x=s(x,N),x.return=b,x)}i(re,"m");function ue(b,x,N){if(typeof x=="string"||typeof x=="number")return x=aa(""+x,b.mode,N),x.return=b,x;if(typeof x=="object"&&x!==null){switch(x.$$typeof){case Ur:return N=Ol(x.type,x.key,x.props,null,b.mode,N),N.ref=Yo(b,null,x),N.return=b,N;case cn:return x=ua(x,b.mode,N),x.return=b,x}if(al(x)||pr(x))return x=Rn(x,b.mode,N,null),x.return=b,x;ul(b,x)}return null}i(ue,"p");function Me(b,x,N,j){var X=x!==null?x.key:null;if(typeof N=="string"||typeof N=="number")return X!==null?null:h(b,x,""+N,j);if(typeof N=="object"&&N!==null){switch(N.$$typeof){case Ur:return N.key===X?N.type===Pt?re(b,x,N.props.children,j,X):L(b,x,N,j):null;case cn:return N.key===X?S(b,x,N,j):null}if(al(N)||pr(N))return X!==null?null:re(b,x,N,j,null);ul(b,N)}return null}i(Me,"x");function Ae(b,x,N,j,X){if(typeof j=="string"||typeof j=="number")return b=b.get(N)||null,h(x,b,""+j,X);if(typeof j=="object"&&j!==null){switch(j.$$typeof){case Ur:return b=b.get(j.key===null?N:j.key)||null,j.type===Pt?re(x,b,j.props.children,X,j.key):L(x,b,j,X);case cn:return b=b.get(j.key===null?N:j.key)||null,S(x,b,j,X)}if(al(j)||pr(j))return b=b.get(N)||null,re(x,b,j,X,null);ul(x,j)}return null}i(Ae,"z");function Rt(b,x,N,j){for(var X=null,ce=null,ye=x,Pe=x=0,Ye=null;ye!==null&&Pe<N.length;Pe++){ye.index>Pe?(Ye=ye,ye=null):Ye=ye.sibling;var Le=Me(b,ye,N[Pe],j);if(Le===null){ye===null&&(ye=Ye);break}e&&ye&&Le.alternate===null&&t(b,ye),x=c(Le,x,Pe),ce===null?X=Le:ce.sibling=Le,ce=Le,ye=Ye}if(Pe===N.length)return n(b,ye),X;if(ye===null){for(;Pe<N.length;Pe++)ye=ue(b,N[Pe],j),ye!==null&&(x=c(ye,x,Pe),ce===null?X=ye:ce.sibling=ye,ce=ye);return X}for(ye=r(b,ye);Pe<N.length;Pe++)Ye=Ae(ye,b,Pe,N[Pe],j),Ye!==null&&(e&&Ye.alternate!==null&&ye.delete(Ye.key===null?Pe:Ye.key),x=c(Ye,x,Pe),ce===null?X=Ye:ce.sibling=Ye,ce=Ye);return e&&ye.forEach(function(Pn){return t(b,Pn)}),X}i(Rt,"ca");function ct(b,x,N,j){var X=pr(N);if(typeof X!="function")throw Error(v(150));if(N=X.call(N),N==null)throw Error(v(151));for(var ce=X=null,ye=x,Pe=x=0,Ye=null,Le=N.next();ye!==null&&!Le.done;Pe++,Le=N.next()){ye.index>Pe?(Ye=ye,ye=null):Ye=ye.sibling;var Pn=Me(b,ye,Le.value,j);if(Pn===null){ye===null&&(ye=Ye);break}e&&ye&&Pn.alternate===null&&t(b,ye),x=c(Pn,x,Pe),ce===null?X=Pn:ce.sibling=Pn,ce=Pn,ye=Ye}if(Le.done)return n(b,ye),X;if(ye===null){for(;!Le.done;Pe++,Le=N.next())Le=ue(b,Le.value,j),Le!==null&&(x=c(Le,x,Pe),ce===null?X=Le:ce.sibling=Le,ce=Le);return X}for(ye=r(b,ye);!Le.done;Pe++,Le=N.next())Le=Ae(ye,b,Pe,Le.value,j),Le!==null&&(e&&Le.alternate!==null&&ye.delete(Le.key===null?Pe:Le.key),x=c(Le,x,Pe),ce===null?X=Le:ce.sibling=Le,ce=Le);return e&&ye.forEach(function(oc){return t(b,oc)}),X}return i(ct,"D"),function(b,x,N,j){var X=typeof N=="object"&&N!==null&&N.type===Pt&&N.key===null;X&&(N=N.props.children);var ce=typeof N=="object"&&N!==null;if(ce)switch(N.$$typeof){case Ur:e:{for(ce=N.key,X=x;X!==null;){if(X.key===ce){switch(X.tag){case 7:if(N.type===Pt){n(b,X.sibling),x=s(X,N.props.children),x.return=b,b=x;break e}break;default:if(X.elementType===N.type){n(b,X.sibling),x=s(X,N.props),x.ref=Yo(b,X,N),x.return=b,b=x;break e}}n(b,X);break}else t(b,X);X=X.sibling}N.type===Pt?(x=Rn(N.props.children,b.mode,j,N.key),x.return=b,b=x):(j=Ol(N.type,N.key,N.props,null,b.mode,j),j.ref=Yo(b,x,N),j.return=b,b=j)}return m(b);case cn:e:{for(X=N.key;x!==null;){if(x.key===X)if(x.tag===4&&x.stateNode.containerInfo===N.containerInfo&&x.stateNode.implementation===N.implementation){n(b,x.sibling),x=s(x,N.children||[]),x.return=b,b=x;break e}else{n(b,x);break}else t(b,x);x=x.sibling}x=ua(N,b.mode,j),x.return=b,b=x}return m(b)}if(typeof N=="string"||typeof N=="number")return N=""+N,x!==null&&x.tag===6?(n(b,x.sibling),x=s(x,N),x.return=b,b=x):(n(b,x),x=aa(N,b.mode,j),x.return=b,b=x),m(b);if(al(N))return Rt(b,x,N,j);if(pr(N))return ct(b,x,N,j);if(ce&&ul(b,N),typeof N=="undefined"&&!X)switch(b.tag){case 1:case 0:throw b=b.type,Error(v(152,b.displayName||b.name||"Component"))}return n(b,x)}}i(Pa,"Rg");var Hr=Pa(!0),Ts=Pa(!1),Go={},Kt={current:Go},Xo={current:Go},Jo={current:Go};function tr(e){if(e===Go)throw Error(v(174));return e}i(tr,"ch");function Ns(e,t){switch(We(Jo,t),We(Xo,e),We(Kt,Go),e=t.nodeType,e){case 9:case 11:t=(t=t.documentElement)?t.namespaceURI:Xr(null,"");break;default:e=e===8?t.parentNode:t,t=e.namespaceURI||null,e=e.tagName,t=Xr(t,e)}Ve(Kt),We(Kt,t)}i(Ns,"dh");function Fr(){Ve(Kt),Ve(Xo),Ve(Jo)}i(Fr,"eh");function Oa(e){tr(Jo.current);var t=tr(Kt.current),n=Xr(t,e.type);t!==n&&(We(Xo,e),We(Kt,n))}i(Oa,"fh");function Ms(e){Xo.current===e&&(Ve(Kt),Ve(Xo))}i(Ms,"gh");var Ge={current:0};function cl(e){for(var t=e;t!==null;){if(t.tag===13){var n=t.memoizedState;if(n!==null&&(n=n.dehydrated,n===null||n.data===Eo||n.data===ko))return t}else if(t.tag===19&&t.memoizedProps.revealOrder!==void 0){if(t.effectTag&64)return t}else if(t.child!==null){t.child.return=t,t=t.child;continue}if(t===e)break;for(;t.sibling===null;){if(t.return===null||t.return===e)return null;t=t.return}t.sibling.return=t.return,t=t.sibling}return null}i(cl,"hh");function Rs(e,t){return{responder:e,props:t}}i(Rs,"ih");var dl=xt.ReactCurrentDispatcher,Mt=xt.ReactCurrentBatchConfig,Sn=0,rt=null,ft=null,mt=null,fl=!1;function kt(){throw Error(v(321))}i(kt,"Q");function Ps(e,t){if(t===null)return!1;for(var n=0;n<t.length&&n<e.length;n++)if(!ln(e[n],t[n]))return!1;return!0}i(Ps,"nh");function Os(e,t,n,r,s,c){if(Sn=c,rt=t,t.memoizedState=null,t.updateQueue=null,t.expirationTime=0,dl.current=e===null||e.memoizedState===null?Ru:Pu,e=n(r,s),t.expirationTime===Sn){c=0;do{if(t.expirationTime=0,!(25>c))throw Error(v(301));c+=1,mt=ft=null,t.updateQueue=null,dl.current=Ou,e=n(r,s)}while(t.expirationTime===Sn)}if(dl.current=gl,t=ft!==null&&ft.next!==null,Sn=0,mt=ft=rt=null,fl=!1,t)throw Error(v(300));return e}i(Os,"oh");function zr(){var e={memoizedState:null,baseState:null,baseQueue:null,queue:null,next:null};return mt===null?rt.memoizedState=mt=e:mt=mt.next=e,mt}i(zr,"th");function Vr(){if(ft===null){var e=rt.alternate;e=e!==null?e.memoizedState:null}else e=ft.next;var t=mt===null?rt.memoizedState:mt.next;if(t!==null)mt=t,ft=e;else{if(e===null)throw Error(v(310));ft=e,e={memoizedState:ft.memoizedState,baseState:ft.baseState,baseQueue:ft.baseQueue,queue:ft.queue,next:null},mt===null?rt.memoizedState=mt=e:mt=mt.next=e}return mt}i(Vr,"uh");function nr(e,t){return typeof t=="function"?t(e):t}i(nr,"vh");function ml(e){var t=Vr(),n=t.queue;if(n===null)throw Error(v(311));n.lastRenderedReducer=e;var r=ft,s=r.baseQueue,c=n.pending;if(c!==null){if(s!==null){var m=s.next;s.next=c.next,c.next=m}r.baseQueue=s=c,n.pending=null}if(s!==null){s=s.next,r=r.baseState;var h=m=c=null,L=s;do{var S=L.expirationTime;if(S<Sn){var re={expirationTime:L.expirationTime,suspenseConfig:L.suspenseConfig,action:L.action,eagerReducer:L.eagerReducer,eagerState:L.eagerState,next:null};h===null?(m=h=re,c=r):h=h.next=re,S>rt.expirationTime&&(rt.expirationTime=S,Pl(S))}else h!==null&&(h=h.next={expirationTime:1073741823,suspenseConfig:L.suspenseConfig,action:L.action,eagerReducer:L.eagerReducer,eagerState:L.eagerState,next:null}),Eu(S,L.suspenseConfig),r=L.eagerReducer===e?L.eagerState:e(r,L.action);L=L.next}while(L!==null&&L!==s);h===null?c=r:h.next=m,ln(r,t.memoizedState)||(Yt=!0),t.memoizedState=r,t.baseState=c,t.baseQueue=h,n.lastRenderedState=r}return[t.memoizedState,n.dispatch]}i(ml,"wh");function pl(e){var t=Vr(),n=t.queue;if(n===null)throw Error(v(311));n.lastRenderedReducer=e;var r=n.dispatch,s=n.pending,c=t.memoizedState;if(s!==null){n.pending=null;var m=s=s.next;do c=e(c,m.action),m=m.next;while(m!==s);ln(c,t.memoizedState)||(Yt=!0),t.memoizedState=c,t.baseQueue===null&&(t.baseState=c),n.lastRenderedState=c}return[c,r]}i(pl,"xh");function Ds(e){var t=zr();return typeof e=="function"&&(e=e()),t.memoizedState=t.baseState=e,e=t.queue={pending:null,dispatch:null,lastRenderedReducer:nr,lastRenderedState:e},e=e.dispatch=$a.bind(null,rt,e),[t.memoizedState,e]}i(Ds,"yh");function As(e,t,n,r){return e={tag:e,create:t,destroy:n,deps:r,next:null},t=rt.updateQueue,t===null?(t={lastEffect:null},rt.updateQueue=t,t.lastEffect=e.next=e):(n=t.lastEffect,n===null?t.lastEffect=e.next=e:(r=n.next,n.next=e,e.next=r,t.lastEffect=e)),e}i(As,"Ah");function Da(){return Vr().memoizedState}i(Da,"Bh");function Is(e,t,n,r){var s=zr();rt.effectTag|=e,s.memoizedState=As(1|t,n,void 0,r===void 0?null:r)}i(Is,"Ch");function Hs(e,t,n,r){var s=Vr();r=r===void 0?null:r;var c=void 0;if(ft!==null){var m=ft.memoizedState;if(c=m.destroy,r!==null&&Ps(r,m.deps)){As(t,n,c,r);return}}rt.effectTag|=e,s.memoizedState=As(1|t,n,c,r)}i(Hs,"Dh");function Aa(e,t){return Is(516,4,e,t)}i(Aa,"Eh");function hl(e,t){return Hs(516,4,e,t)}i(hl,"Fh");function Ia(e,t){return Hs(4,2,e,t)}i(Ia,"Gh");function Ha(e,t){if(typeof t=="function")return e=e(),t(e),function(){t(null)};if(t!=null)return e=e(),t.current=e,function(){t.current=null}}i(Ha,"Hh");function Fa(e,t,n){return n=n!=null?n.concat([e]):null,Hs(4,2,Ha.bind(null,t,e),n)}i(Fa,"Ih");function Fs(){}i(Fs,"Jh");function za(e,t){return zr().memoizedState=[e,t===void 0?null:t],e}i(za,"Kh");function vl(e,t){var n=Vr();t=t===void 0?null:t;var r=n.memoizedState;return r!==null&&t!==null&&Ps(t,r[1])?r[0]:(n.memoizedState=[e,t],e)}i(vl,"Lh");function Va(e,t){var n=Vr();t=t===void 0?null:t;var r=n.memoizedState;return r!==null&&t!==null&&Ps(t,r[1])?r[0]:(e=e(),n.memoizedState=[e,t],e)}i(Va,"Mh");function zs(e,t,n){var r=tl();kn(98>r?98:r,function(){e(!0)}),kn(97<r?97:r,function(){var s=Mt.suspense;Mt.suspense=t===void 0?null:t;try{e(!1),n()}finally{Mt.suspense=s}})}i(zs,"Nh");function $a(e,t,n){var r=Xt(),s=Ko.suspense;r=lr(r,e,s),s={expirationTime:r,suspenseConfig:s,action:n,eagerReducer:null,eagerState:null,next:null};var c=t.pending;if(c===null?s.next=s:(s.next=c.next,c.next=s),t.pending=s,c=e.alternate,e===rt||c!==null&&c===rt)fl=!0,s.expirationTime=Sn,rt.expirationTime=Sn;else{if(e.expirationTime===0&&(c===null||c.expirationTime===0)&&(c=t.lastRenderedReducer,c!==null))try{var m=t.lastRenderedState,h=c(m,n);if(s.eagerReducer=c,s.eagerState=h,ln(h,m))return}catch{}finally{}Mn(e,r)}}i($a,"zh");var gl={readContext:Nt,useCallback:kt,useContext:kt,useEffect:kt,useImperativeHandle:kt,useLayoutEffect:kt,useMemo:kt,useReducer:kt,useRef:kt,useState:kt,useDebugValue:kt,useResponder:kt,useDeferredValue:kt,useTransition:kt},Ru={readContext:Nt,useCallback:za,useContext:Nt,useEffect:Aa,useImperativeHandle:i(function(e,t,n){return n=n!=null?n.concat([e]):null,Is(4,2,Ha.bind(null,t,e),n)},"useImperativeHandle"),useLayoutEffect:i(function(e,t){return Is(4,2,e,t)},"useLayoutEffect"),useMemo:i(function(e,t){var n=zr();return t=t===void 0?null:t,e=e(),n.memoizedState=[e,t],e},"useMemo"),useReducer:i(function(e,t,n){var r=zr();return t=n!==void 0?n(t):t,r.memoizedState=r.baseState=t,e=r.queue={pending:null,dispatch:null,lastRenderedReducer:e,lastRenderedState:t},e=e.dispatch=$a.bind(null,rt,e),[r.memoizedState,e]},"useReducer"),useRef:i(function(e){var t=zr();return e={current:e},t.memoizedState=e},"useRef"),useState:Ds,useDebugValue:Fs,useResponder:Rs,useDeferredValue:i(function(e,t){var n=Ds(e),r=n[0],s=n[1];return Aa(function(){var c=Mt.suspense;Mt.suspense=t===void 0?null:t;try{s(e)}finally{Mt.suspense=c}},[e,t]),r},"useDeferredValue"),useTransition:i(function(e){var t=Ds(!1),n=t[0];return t=t[1],[za(zs.bind(null,t,e),[t,e]),n]},"useTransition")},Pu={readContext:Nt,useCallback:vl,useContext:Nt,useEffect:hl,useImperativeHandle:Fa,useLayoutEffect:Ia,useMemo:Va,useReducer:ml,useRef:Da,useState:i(function(){return ml(nr)},"useState"),useDebugValue:Fs,useResponder:Rs,useDeferredValue:i(function(e,t){var n=ml(nr),r=n[0],s=n[1];return hl(function(){var c=Mt.suspense;Mt.suspense=t===void 0?null:t;try{s(e)}finally{Mt.suspense=c}},[e,t]),r},"useDeferredValue"),useTransition:i(function(e){var t=ml(nr),n=t[0];return t=t[1],[vl(zs.bind(null,t,e),[t,e]),n]},"useTransition")},Ou={readContext:Nt,useCallback:vl,useContext:Nt,useEffect:hl,useImperativeHandle:Fa,useLayoutEffect:Ia,useMemo:Va,useReducer:pl,useRef:Da,useState:i(function(){return pl(nr)},"useState"),useDebugValue:Fs,useResponder:Rs,useDeferredValue:i(function(e,t){var n=pl(nr),r=n[0],s=n[1];return hl(function(){var c=Mt.suspense;Mt.suspense=t===void 0?null:t;try{s(e)}finally{Mt.suspense=c}},[e,t]),r},"useDeferredValue"),useTransition:i(function(e){var t=pl(nr),n=t[0];return t=t[1],[vl(zs.bind(null,t,e),[t,e]),n]},"useTransition")},sn=null,Tn=null,rr=!1;function ja(e,t){var n=Jt(5,null,null,0);n.elementType="DELETED",n.type="DELETED",n.stateNode=t,n.return=e,n.effectTag=8,e.lastEffect!==null?(e.lastEffect.nextEffect=n,e.lastEffect=n):e.firstEffect=e.lastEffect=n}i(ja,"Rh");function Ba(e,t){switch(e.tag){case 5:var n=e.type;return t=t.nodeType!==1||n.toLowerCase()!==t.nodeName.toLowerCase()?null:t,t!==null?(e.stateNode=t,!0):!1;case 6:return t=e.pendingProps===""||t.nodeType!==3?null:t,t!==null?(e.stateNode=t,!0):!1;case 13:return!1;default:return!1}}i(Ba,"Th");function Vs(e){if(rr){var t=Tn;if(t){var n=t;if(!Ba(e,t)){if(t=gn(n.nextSibling),!t||!Ba(e,t)){e.effectTag=e.effectTag&-1025|2,rr=!1,sn=e;return}ja(sn,n)}sn=e,Tn=gn(t.firstChild)}else e.effectTag=e.effectTag&-1025|2,rr=!1,sn=e}}i(Vs,"Uh");function Ua(e){for(e=e.return;e!==null&&e.tag!==5&&e.tag!==3&&e.tag!==13;)e=e.return;sn=e}i(Ua,"Vh");function yl(e){if(e!==sn)return!1;if(!rr)return Ua(e),rr=!0,!1;var t=e.type;if(e.tag!==5||t!=="head"&&t!=="body"&&!Je(t,e.memoizedProps))for(t=Tn;t;)ja(e,t),t=gn(t.nextSibling);if(Ua(e),e.tag===13){if(e=e.memoizedState,e=e!==null?e.dehydrated:null,!e)throw Error(v(317));e:{for(e=e.nextSibling,t=0;e;){if(e.nodeType===8){var n=e.data;if(n===jn){if(t===0){Tn=gn(e.nextSibling);break e}t--}else n!==Di&&n!==ko&&n!==Eo||t++}e=e.nextSibling}Tn=null}}else Tn=sn?gn(e.stateNode.nextSibling):null;return!0}i(yl,"Wh");function $s(){Tn=sn=null,rr=!1}i($s,"Xh");var Du=xt.ReactCurrentOwner,Yt=!1;function bt(e,t,n,r){t.child=e===null?Ts(t,null,n,r):Hr(t,e.child,n,r)}i(bt,"R");function Wa(e,t,n,r,s){n=n.render;var c=t.ref;return Ir(t,s),r=Os(e,t,n,r,c,s),e!==null&&!Yt?(t.updateQueue=e.updateQueue,t.effectTag&=-517,e.expirationTime<=s&&(e.expirationTime=0),an(e,t,s)):(t.effectTag|=1,bt(e,t,r,s),t.child)}i(Wa,"Zh");function qa(e,t,n,r,s,c){if(e===null){var m=n.type;return typeof m=="function"&&!sa(m)&&m.defaultProps===void 0&&n.compare===null&&n.defaultProps===void 0?(t.tag=15,t.type=m,Za(e,t,m,r,s,c)):(e=Ol(n.type,null,r,null,t.mode,c),e.ref=t.ref,e.return=t,t.child=e)}return m=e.child,s<c&&(s=m.memoizedProps,n=n.compare,n=n!==null?n:Jn,n(s,r)&&e.ref===t.ref)?an(e,t,c):(t.effectTag|=1,e=cr(m,r),e.ref=t.ref,e.return=t,t.child=e)}i(qa,"ai");function Za(e,t,n,r,s,c){return e!==null&&Jn(e.memoizedProps,r)&&e.ref===t.ref&&(Yt=!1,s<c)?(t.expirationTime=e.expirationTime,an(e,t,c)):js(e,t,n,r,c)}i(Za,"ci");function Qa(e,t){var n=t.ref;(e===null&&n!==null||e!==null&&e.ref!==n)&&(t.effectTag|=128)}i(Qa,"ei");function js(e,t,n,r,s){var c=f(n)?u:o.current;return c=d(t,c),Ir(t,s),n=Os(e,t,n,r,c,s),e!==null&&!Yt?(t.updateQueue=e.updateQueue,t.effectTag&=-517,e.expirationTime<=s&&(e.expirationTime=0),an(e,t,s)):(t.effectTag|=1,bt(e,t,n,s),t.child)}i(js,"di");function Ka(e,t,n,r,s){if(f(n)){var c=!0;k(t)}else c=!1;if(Ir(t,s),t.stateNode===null)e!==null&&(e.alternate=null,t.alternate=null,t.effectTag|=2),Ma(t,n,r),Ss(t,n,r,s),r=!0;else if(e===null){var m=t.stateNode,h=t.memoizedProps;m.props=h;var L=m.context,S=n.contextType;typeof S=="object"&&S!==null?S=Nt(S):(S=f(n)?u:o.current,S=d(t,S));var re=n.getDerivedStateFromProps,ue=typeof re=="function"||typeof m.getSnapshotBeforeUpdate=="function";ue||typeof m.UNSAFE_componentWillReceiveProps!="function"&&typeof m.componentWillReceiveProps!="function"||(h!==r||L!==S)&&Ra(t,m,r,S),bn=!1;var Me=t.memoizedState;m.state=Me,Qo(t,r,m,s),L=t.memoizedState,h!==r||Me!==L||a.current||bn?(typeof re=="function"&&(ll(t,n,re,r),L=t.memoizedState),(h=bn||Na(t,n,h,r,Me,L,S))?(ue||typeof m.UNSAFE_componentWillMount!="function"&&typeof m.componentWillMount!="function"||(typeof m.componentWillMount=="function"&&m.componentWillMount(),typeof m.UNSAFE_componentWillMount=="function"&&m.UNSAFE_componentWillMount()),typeof m.componentDidMount=="function"&&(t.effectTag|=4)):(typeof m.componentDidMount=="function"&&(t.effectTag|=4),t.memoizedProps=r,t.memoizedState=L),m.props=r,m.state=L,m.context=S,r=h):(typeof m.componentDidMount=="function"&&(t.effectTag|=4),r=!1)}else m=t.stateNode,Ls(e,t),h=t.memoizedProps,m.props=t.type===t.elementType?h:Vt(t.type,h),L=m.context,S=n.contextType,typeof S=="object"&&S!==null?S=Nt(S):(S=f(n)?u:o.current,S=d(t,S)),re=n.getDerivedStateFromProps,(ue=typeof re=="function"||typeof m.getSnapshotBeforeUpdate=="function")||typeof m.UNSAFE_componentWillReceiveProps!="function"&&typeof m.componentWillReceiveProps!="function"||(h!==r||L!==S)&&Ra(t,m,r,S),bn=!1,L=t.memoizedState,m.state=L,Qo(t,r,m,s),Me=t.memoizedState,h!==r||L!==Me||a.current||bn?(typeof re=="function"&&(ll(t,n,re,r),Me=t.memoizedState),(re=bn||Na(t,n,h,r,L,Me,S))?(ue||typeof m.UNSAFE_componentWillUpdate!="function"&&typeof m.componentWillUpdate!="function"||(typeof m.componentWillUpdate=="function"&&m.componentWillUpdate(r,Me,S),typeof m.UNSAFE_componentWillUpdate=="function"&&m.UNSAFE_componentWillUpdate(r,Me,S)),typeof m.componentDidUpdate=="function"&&(t.effectTag|=4),typeof m.getSnapshotBeforeUpdate=="function"&&(t.effectTag|=256)):(typeof m.componentDidUpdate!="function"||h===e.memoizedProps&&L===e.memoizedState||(t.effectTag|=4),typeof m.getSnapshotBeforeUpdate!="function"||h===e.memoizedProps&&L===e.memoizedState||(t.effectTag|=256),t.memoizedProps=r,t.memoizedState=Me),m.props=r,m.state=Me,m.context=S,r=re):(typeof m.componentDidUpdate!="function"||h===e.memoizedProps&&L===e.memoizedState||(t.effectTag|=4),typeof m.getSnapshotBeforeUpdate!="function"||h===e.memoizedProps&&L===e.memoizedState||(t.effectTag|=256),r=!1);return Bs(e,t,n,r,c,s)}i(Ka,"fi");function Bs(e,t,n,r,s,c){Qa(e,t);var m=(t.effectTag&64)!==0;if(!r&&!m)return s&&D(t,n,!1),an(e,t,c);r=t.stateNode,Du.current=t;var h=m&&typeof n.getDerivedStateFromError!="function"?null:r.render();return t.effectTag|=1,e!==null&&m?(t.child=Hr(t,e.child,null,c),t.child=Hr(t,null,h,c)):bt(e,t,h,c),t.memoizedState=r.state,s&&D(t,n,!0),t.child}i(Bs,"gi");function Ya(e){var t=e.stateNode;t.pendingContext?g(e,t.pendingContext,t.pendingContext!==t.context):t.context&&g(e,t.context,!1),Ns(e,t.containerInfo)}i(Ya,"hi");var Us={dehydrated:null,retryTime:0};function Ga(e,t,n){var r=t.mode,s=t.pendingProps,c=Ge.current,m=!1,h;if((h=(t.effectTag&64)!==0)||(h=(c&2)!==0&&(e===null||e.memoizedState!==null)),h?(m=!0,t.effectTag&=-65):e!==null&&e.memoizedState===null||s.fallback===void 0||s.unstable_avoidThisFallback===!0||(c|=1),We(Ge,c&1),e===null){if(s.fallback!==void 0&&Vs(t),m){if(m=s.fallback,s=Rn(null,r,0,null),s.return=t,!(t.mode&2))for(e=t.memoizedState!==null?t.child.child:t.child,s.child=e;e!==null;)e.return=s,e=e.sibling;return n=Rn(m,r,n,null),n.return=t,s.sibling=n,t.memoizedState=Us,t.child=s,n}return r=s.children,t.memoizedState=null,t.child=Ts(t,null,r,n)}if(e.memoizedState!==null){if(e=e.child,r=e.sibling,m){if(s=s.fallback,n=cr(e,e.pendingProps),n.return=t,!(t.mode&2)&&(m=t.memoizedState!==null?t.child.child:t.child,m!==e.child))for(n.child=m;m!==null;)m.return=n,m=m.sibling;return r=cr(r,s),r.return=t,n.sibling=r,n.childExpirationTime=0,t.memoizedState=Us,t.child=n,r}return n=Hr(t,e.child,s.children,n),t.memoizedState=null,t.child=n}if(e=e.child,m){if(m=s.fallback,s=Rn(null,r,0,null),s.return=t,s.child=e,e!==null&&(e.return=s),!(t.mode&2))for(e=t.memoizedState!==null?t.child.child:t.child,s.child=e;e!==null;)e.return=s,e=e.sibling;return n=Rn(m,r,n,null),n.return=t,s.sibling=n,n.effectTag|=2,s.childExpirationTime=0,t.memoizedState=Us,t.child=s,n}return t.memoizedState=null,t.child=Hr(t,e,s.children,n)}i(Ga,"ji");function Xa(e,t){e.expirationTime<t&&(e.expirationTime=t);var n=e.alternate;n!==null&&n.expirationTime<t&&(n.expirationTime=t),_a(e.return,t)}i(Xa,"ki");function Ws(e,t,n,r,s,c){var m=e.memoizedState;m===null?e.memoizedState={isBackwards:t,rendering:null,renderingStartTime:0,last:r,tail:n,tailExpiration:0,tailMode:s,lastEffect:c}:(m.isBackwards=t,m.rendering=null,m.renderingStartTime=0,m.last=r,m.tail=n,m.tailExpiration=0,m.tailMode=s,m.lastEffect=c)}i(Ws,"li");function Ja(e,t,n){var r=t.pendingProps,s=r.revealOrder,c=r.tail;if(bt(e,t,r.children,n),r=Ge.current,r&2)r=r&1|2,t.effectTag|=64;else{if(e!==null&&e.effectTag&64)e:for(e=t.child;e!==null;){if(e.tag===13)e.memoizedState!==null&&Xa(e,n);else if(e.tag===19)Xa(e,n);else if(e.child!==null){e.child.return=e,e=e.child;continue}if(e===t)break e;for(;e.sibling===null;){if(e.return===null||e.return===t)break e;e=e.return}e.sibling.return=e.return,e=e.sibling}r&=1}if(We(Ge,r),!(t.mode&2))t.memoizedState=null;else switch(s){case"forwards":for(n=t.child,s=null;n!==null;)e=n.alternate,e!==null&&cl(e)===null&&(s=n),n=n.sibling;n=s,n===null?(s=t.child,t.child=null):(s=n.sibling,n.sibling=null),Ws(t,!1,s,n,c,t.lastEffect);break;case"backwards":for(n=null,s=t.child,t.child=null;s!==null;){if(e=s.alternate,e!==null&&cl(e)===null){t.child=s;break}e=s.sibling,s.sibling=n,n=s,s=e}Ws(t,!0,n,null,c,t.lastEffect);break;case"together":Ws(t,!1,null,null,void 0,t.lastEffect);break;default:t.memoizedState=null}return t.child}i(Ja,"mi");function an(e,t,n){e!==null&&(t.dependencies=e.dependencies);var r=t.expirationTime;if(r!==0&&Pl(r),t.childExpirationTime<n)return null;if(e!==null&&t.child!==e.child)throw Error(v(153));if(t.child!==null){for(e=t.child,n=cr(e,e.pendingProps),t.child=n,n.return=t;e.sibling!==null;)e=e.sibling,n=n.sibling=cr(e,e.pendingProps),n.return=t;n.sibling=null}return t.child}i(an,"$h");var eu,qs,tu,nu;eu=i(function(e,t){for(var n=t.child;n!==null;){if(n.tag===5||n.tag===6)e.appendChild(n.stateNode);else if(n.tag!==4&&n.child!==null){n.child.return=n,n=n.child;continue}if(n===t)break;for(;n.sibling===null;){if(n.return===null||n.return===t)return;n=n.return}n.sibling.return=n.return,n=n.sibling}},"ni"),qs=i(function(){},"oi"),tu=i(function(e,t,n,r,s){var c=e.memoizedProps;if(c!==r){var m=t.stateNode;switch(tr(Kt.current),e=null,n){case"input":c=Zr(m,c),r=Zr(m,r),e=[];break;case"option":c=Yr(m,c),r=Yr(m,r),e=[];break;case"select":c=I({},c,{value:void 0}),r=I({},r,{value:void 0}),e=[];break;case"textarea":c=Gr(m,c),r=Gr(m,r),e=[];break;default:typeof c.onClick!="function"&&typeof r.onClick=="function"&&(m.onclick=Tr)}go(n,r);var h,L;n=null;for(h in c)if(!r.hasOwnProperty(h)&&c.hasOwnProperty(h)&&c[h]!=null)if(h==="style")for(L in m=c[h],m)m.hasOwnProperty(L)&&(n||(n={}),n[L]="");else h!=="dangerouslySetInnerHTML"&&h!=="children"&&h!=="suppressContentEditableWarning"&&h!=="suppressHydrationWarning"&&h!=="autoFocus"&&(A.hasOwnProperty(h)?e||(e=[]):(e=e||[]).push(h,null));for(h in r){var S=r[h];if(m=c!=null?c[h]:void 0,r.hasOwnProperty(h)&&S!==m&&(S!=null||m!=null))if(h==="style")if(m){for(L in m)!m.hasOwnProperty(L)||S&&S.hasOwnProperty(L)||(n||(n={}),n[L]="");for(L in S)S.hasOwnProperty(L)&&m[L]!==S[L]&&(n||(n={}),n[L]=S[L])}else n||(e||(e=[]),e.push(h,n)),n=S;else h==="dangerouslySetInnerHTML"?(S=S?S.__html:void 0,m=m?m.__html:void 0,S!=null&&m!==S&&(e=e||[]).push(h,S)):h==="children"?m===S||typeof S!="string"&&typeof S!="number"||(e=e||[]).push(h,""+S):h!=="suppressContentEditableWarning"&&h!=="suppressHydrationWarning"&&(A.hasOwnProperty(h)?(S!=null&&It(s,h),e||m===S||(e=[])):(e=e||[]).push(h,S))}n&&(e=e||[]).push("style",n),s=e,(t.updateQueue=s)&&(t.effectTag|=4)}},"pi"),nu=i(function(e,t,n,r){n!==r&&(t.effectTag|=4)},"qi");function Cl(e,t){switch(e.tailMode){case"hidden":t=e.tail;for(var n=null;t!==null;)t.alternate!==null&&(n=t),t=t.sibling;n===null?e.tail=null:n.sibling=null;break;case"collapsed":n=e.tail;for(var r=null;n!==null;)n.alternate!==null&&(r=n),n=n.sibling;r===null?t||e.tail===null?e.tail=null:e.tail.sibling=null:r.sibling=null}}i(Cl,"ri");function Au(e,t,n){var r=t.pendingProps;switch(t.tag){case 2:case 16:case 15:case 0:case 11:case 7:case 8:case 12:case 9:case 14:return null;case 1:return f(t.type)&&p(),null;case 3:return Fr(),Ve(a),Ve(o),n=t.stateNode,n.pendingContext&&(n.context=n.pendingContext,n.pendingContext=null),e!==null&&e.child!==null||!yl(t)||(t.effectTag|=4),qs(t),null;case 5:Ms(t),n=tr(Jo.current);var s=t.type;if(e!==null&&t.stateNode!=null)tu(e,t,s,r,n),e.ref!==t.ref&&(t.effectTag|=128);else{if(!r){if(t.stateNode===null)throw Error(v(166));return null}if(e=tr(Kt.current),yl(t)){r=t.stateNode,s=t.type;var c=t.memoizedProps;switch(r[Ut]=t,r[Nr]=c,s){case"iframe":case"object":case"embed":je("load",r);break;case"video":case"audio":for(e=0;e<vr.length;e++)je(vr[e],r);break;case"source":je("error",r);break;case"img":case"image":case"link":je("error",r),je("load",r);break;case"form":je("reset",r),je("submit",r);break;case"details":je("toggle",r);break;case"input":di(r,c),je("invalid",r),It(n,"onChange");break;case"select":r._wrapperState={wasMultiple:!!c.multiple},je("invalid",r),It(n,"onChange");break;case"textarea":Ul(r,c),je("invalid",r),It(n,"onChange")}go(s,c),e=null;for(var m in c)if(c.hasOwnProperty(m)){var h=c[m];m==="children"?typeof h=="string"?r.textContent!==h&&(e=["children",h]):typeof h=="number"&&r.textContent!==""+h&&(e=["children",""+h]):A.hasOwnProperty(m)&&h!=null&&It(n,m)}switch(s){case"input":qr(r),mi(r,c,!0);break;case"textarea":qr(r),On(r);break;case"select":case"option":break;default:typeof c.onClick=="function"&&(r.onclick=Tr)}n=e,t.updateQueue=n,n!==null&&(t.effectTag|=4)}else{switch(m=n.nodeType===9?n:n.ownerDocument,e===Mi&&(e=vi(s)),e===Mi?s==="script"?(e=m.createElement("div"),e.innerHTML="<script><\/script>",e=e.removeChild(e.firstChild)):typeof r.is=="string"?e=m.createElement(s,{is:r.is}):(e=m.createElement(s),s==="select"&&(m=e,r.multiple?m.multiple=!0:r.size&&(m.size=r.size))):e=m.createElementNS(e,s),e[Ut]=t,e[Nr]=r,eu(e,t,!1,!1),t.stateNode=e,m=yo(s,r),s){case"iframe":case"object":case"embed":je("load",e),h=r;break;case"video":case"audio":for(h=0;h<vr.length;h++)je(vr[h],e);h=r;break;case"source":je("error",e),h=r;break;case"img":case"image":case"link":je("error",e),je("load",e),h=r;break;case"form":je("reset",e),je("submit",e),h=r;break;case"details":je("toggle",e),h=r;break;case"input":di(e,r),h=Zr(e,r),je("invalid",e),It(n,"onChange");break;case"option":h=Yr(e,r);break;case"select":e._wrapperState={wasMultiple:!!r.multiple},h=I({},r,{value:void 0}),je("invalid",e),It(n,"onChange");break;case"textarea":Ul(e,r),h=Gr(e,r),je("invalid",e),It(n,"onChange");break;default:h=r}go(s,h);var L=h;for(c in L)if(L.hasOwnProperty(c)){var S=L[c];c==="style"?Ni(e,S):c==="dangerouslySetInnerHTML"?(S=S?S.__html:void 0,S!=null&&gi(e,S)):c==="children"?typeof S=="string"?(s!=="textarea"||S!=="")&&Dn(e,S):typeof S=="number"&&Dn(e,""+S):c!=="suppressContentEditableWarning"&&c!=="suppressHydrationWarning"&&c!=="autoFocus"&&(A.hasOwnProperty(c)?S!=null&&It(n,c):S!=null&&Br(e,c,S,m))}switch(s){case"input":qr(e),mi(e,r,!1);break;case"textarea":qr(e),On(e);break;case"option":r.value!=null&&e.setAttribute("value",""+tn(r.value));break;case"select":e.multiple=!!r.multiple,n=r.value,n!=null?Ue(e,!!r.multiple,n,!1):r.defaultValue!=null&&Ue(e,!!r.multiple,r.defaultValue,!0);break;default:typeof h.onClick=="function"&&(e.onclick=Tr)}Ai(s,r)&&(t.effectTag|=4)}t.ref!==null&&(t.effectTag|=128)}return null;case 6:if(e&&t.stateNode!=null)nu(e,t,e.memoizedProps,r);else{if(typeof r!="string"&&t.stateNode===null)throw Error(v(166));n=tr(Jo.current),tr(Kt.current),yl(t)?(n=t.stateNode,r=t.memoizedProps,n[Ut]=t,n.nodeValue!==r&&(t.effectTag|=4)):(n=(n.nodeType===9?n:n.ownerDocument).createTextNode(r),n[Ut]=t,t.stateNode=n)}return null;case 13:return Ve(Ge),r=t.memoizedState,t.effectTag&64?(t.expirationTime=n,t):(n=r!==null,r=!1,e===null?t.memoizedProps.fallback!==void 0&&yl(t):(s=e.memoizedState,r=s!==null,n||s===null||(s=e.child.sibling,s!==null&&(c=t.firstEffect,c!==null?(t.firstEffect=s,s.nextEffect=c):(t.firstEffect=t.lastEffect=s,s.nextEffect=null),s.effectTag=8))),n&&!r&&t.mode&2&&(e===null&&t.memoizedProps.unstable_avoidThisFallback!==!0||Ge.current&1?lt===or&&(lt=El):((lt===or||lt===El)&&(lt=kl),ti!==0&&_t!==null&&(dr(_t,wt),Tu(_t,ti)))),(n||r)&&(t.effectTag|=4),null);case 4:return Fr(),qs(t),null;case 10:return bs(t),null;case 17:return f(t.type)&&p(),null;case 19:if(Ve(Ge),r=t.memoizedState,r===null)return null;if(s=(t.effectTag&64)!==0,c=r.rendering,c===null){if(s)Cl(r,!1);else if(lt!==or||e!==null&&e.effectTag&64)for(c=t.child;c!==null;){if(e=cl(c),e!==null){for(t.effectTag|=64,Cl(r,!1),s=e.updateQueue,s!==null&&(t.updateQueue=s,t.effectTag|=4),r.lastEffect===null&&(t.firstEffect=null),t.lastEffect=r.lastEffect,r=t.child;r!==null;)s=r,c=n,s.effectTag&=2,s.nextEffect=null,s.firstEffect=null,s.lastEffect=null,e=s.alternate,e===null?(s.childExpirationTime=0,s.expirationTime=c,s.child=null,s.memoizedProps=null,s.memoizedState=null,s.updateQueue=null,s.dependencies=null):(s.childExpirationTime=e.childExpirationTime,s.expirationTime=e.expirationTime,s.child=e.child,s.memoizedProps=e.memoizedProps,s.memoizedState=e.memoizedState,s.updateQueue=e.updateQueue,c=e.dependencies,s.dependencies=c===null?null:{expirationTime:c.expirationTime,firstContext:c.firstContext,responders:c.responders}),r=r.sibling;return We(Ge,Ge.current&1|2),t.child}c=c.sibling}}else{if(!s)if(e=cl(c),e!==null){if(t.effectTag|=64,s=!0,n=e.updateQueue,n!==null&&(t.updateQueue=n,t.effectTag|=4),Cl(r,!0),r.tail===null&&r.tailMode==="hidden"&&!c.alternate)return t=t.lastEffect=r.lastEffect,t!==null&&(t.nextEffect=null),null}else 2*Ct()-r.renderingStartTime>r.tailExpiration&&1<n&&(t.effectTag|=64,s=!0,Cl(r,!1),t.expirationTime=t.childExpirationTime=n-1);r.isBackwards?(c.sibling=t.child,t.child=c):(n=r.last,n!==null?n.sibling=c:t.child=c,r.last=c)}return r.tail!==null?(r.tailExpiration===0&&(r.tailExpiration=Ct()+500),n=r.tail,r.rendering=n,r.tail=n.sibling,r.lastEffect=t.lastEffect,r.renderingStartTime=Ct(),n.sibling=null,t=Ge.current,We(Ge,s?t&1|2:t&1),n):null}throw Error(v(156,t.tag))}i(Au,"si");function Iu(e){switch(e.tag){case 1:f(e.type)&&p();var t=e.effectTag;return t&4096?(e.effectTag=t&-4097|64,e):null;case 3:if(Fr(),Ve(a),Ve(o),t=e.effectTag,t&64)throw Error(v(285));return e.effectTag=t&-4097|64,e;case 5:return Ms(e),null;case 13:return Ve(Ge),t=e.effectTag,t&4096?(e.effectTag=t&-4097|64,e):null;case 19:return Ve(Ge),null;case 4:return Fr(),null;case 10:return bs(e),null;default:return null}}i(Iu,"zi");function Zs(e,t){return{value:e,source:t,stack:ci(t)}}i(Zs,"Ai");var Hu=typeof WeakSet=="function"?WeakSet:Set;function Qs(e,t){var n=t.source,r=t.stack;r===null&&n!==null&&(r=ci(n)),n!==null&&jt(n.type),t=t.value,e!==null&&e.tag===1&&jt(e.type);try{console.error(t)}catch(s){setTimeout(function(){throw s})}}i(Qs,"Ci");function Fu(e,t){try{t.props=e.memoizedProps,t.state=e.memoizedState,t.componentWillUnmount()}catch(n){ur(e,n)}}i(Fu,"Di");function ru(e){var t=e.ref;if(t!==null)if(typeof t=="function")try{t(null)}catch(n){ur(e,n)}else t.current=null}i(ru,"Fi");function zu(e,t){switch(t.tag){case 0:case 11:case 15:case 22:return;case 1:if(t.effectTag&256&&e!==null){var n=e.memoizedProps,r=e.memoizedState;e=t.stateNode,t=e.getSnapshotBeforeUpdate(t.elementType===t.type?n:Vt(t.type,n),r),e.__reactInternalSnapshotBeforeUpdate=t}return;case 3:case 5:case 6:case 4:case 17:return}throw Error(v(163))}i(zu,"Gi");function ou(e,t){if(t=t.updateQueue,t=t!==null?t.lastEffect:null,t!==null){var n=t=t.next;do{if((n.tag&e)===e){var r=n.destroy;n.destroy=void 0,r!==void 0&&r()}n=n.next}while(n!==t)}}i(ou,"Hi");function iu(e,t){if(t=t.updateQueue,t=t!==null?t.lastEffect:null,t!==null){var n=t=t.next;do{if((n.tag&e)===e){var r=n.create;n.destroy=r()}n=n.next}while(n!==t)}}i(iu,"Ii");function Vu(e,t,n){switch(n.tag){case 0:case 11:case 15:case 22:iu(3,n);return;case 1:if(e=n.stateNode,n.effectTag&4)if(t===null)e.componentDidMount();else{var r=n.elementType===n.type?t.memoizedProps:Vt(n.type,t.memoizedProps);e.componentDidUpdate(r,t.memoizedState,e.__reactInternalSnapshotBeforeUpdate)}t=n.updateQueue,t!==null&&Sa(n,t,e);return;case 3:if(t=n.updateQueue,t!==null){if(e=null,n.child!==null)switch(n.child.tag){case 5:e=n.child.stateNode;break;case 1:e=n.child.stateNode}Sa(n,t,e)}return;case 5:e=n.stateNode,t===null&&n.effectTag&4&&Ai(n.type,n.memoizedProps)&&e.focus();return;case 6:return;case 4:return;case 12:return;case 13:n.memoizedState===null&&(n=n.alternate,n!==null&&(n=n.memoizedState,n!==null&&(n=n.dehydrated,n!==null&&Li(n))));return;case 19:case 17:case 20:case 21:return}throw Error(v(163))}i(Vu,"Ji");function lu(e,t,n){switch(typeof la=="function"&&la(t),t.tag){case 0:case 11:case 14:case 15:case 22:if(e=t.updateQueue,e!==null&&(e=e.lastEffect,e!==null)){var r=e.next;kn(97<n?97:n,function(){var s=r;do{var c=s.destroy;if(c!==void 0){var m=t;try{c()}catch(h){ur(m,h)}}s=s.next}while(s!==r)})}break;case 1:ru(t),n=t.stateNode,typeof n.componentWillUnmount=="function"&&Fu(t,n);break;case 5:ru(t);break;case 4:cu(e,t,n)}}i(lu,"Ki");function su(e){var t=e.alternate;e.return=null,e.child=null,e.memoizedState=null,e.updateQueue=null,e.dependencies=null,e.alternate=null,e.firstEffect=null,e.lastEffect=null,e.pendingProps=null,e.memoizedProps=null,e.stateNode=null,t!==null&&su(t)}i(su,"Ni");function au(e){return e.tag===5||e.tag===3||e.tag===4}i(au,"Oi");function uu(e){e:{for(var t=e.return;t!==null;){if(au(t)){var n=t;break e}t=t.return}throw Error(v(160))}switch(t=n.stateNode,n.tag){case 5:var r=!1;break;case 3:t=t.containerInfo,r=!0;break;case 4:t=t.containerInfo,r=!0;break;default:throw Error(v(161))}n.effectTag&16&&(Dn(t,""),n.effectTag&=-17);e:t:for(n=e;;){for(;n.sibling===null;){if(n.return===null||au(n.return)){n=null;break e}n=n.return}for(n.sibling.return=n.return,n=n.sibling;n.tag!==5&&n.tag!==6&&n.tag!==18;){if(n.effectTag&2||n.child===null||n.tag===4)continue t;n.child.return=n,n=n.child}if(!(n.effectTag&2)){n=n.stateNode;break e}}r?Ks(e,n,t):Ys(e,n,t)}i(uu,"Pi");function Ks(e,t,n){var r=e.tag,s=r===5||r===6;if(s)e=s?e.stateNode:e.stateNode.instance,t?n.nodeType===8?n.parentNode.insertBefore(e,t):n.insertBefore(e,t):(n.nodeType===8?(t=n.parentNode,t.insertBefore(e,n)):(t=n,t.appendChild(e)),n=n._reactRootContainer,n!=null||t.onclick!==null||(t.onclick=Tr));else if(r!==4&&(e=e.child,e!==null))for(Ks(e,t,n),e=e.sibling;e!==null;)Ks(e,t,n),e=e.sibling}i(Ks,"Qi");function Ys(e,t,n){var r=e.tag,s=r===5||r===6;if(s)e=s?e.stateNode:e.stateNode.instance,t?n.insertBefore(e,t):n.appendChild(e);else if(r!==4&&(e=e.child,e!==null))for(Ys(e,t,n),e=e.sibling;e!==null;)Ys(e,t,n),e=e.sibling}i(Ys,"Ri");function cu(e,t,n){for(var r=t,s=!1,c,m;;){if(!s){s=r.return;e:for(;;){if(s===null)throw Error(v(160));switch(c=s.stateNode,s.tag){case 5:m=!1;break e;case 3:c=c.containerInfo,m=!0;break e;case 4:c=c.containerInfo,m=!0;break e}s=s.return}s=!0}if(r.tag===5||r.tag===6){e:for(var h=e,L=r,S=n,re=L;;)if(lu(h,re,S),re.child!==null&&re.tag!==4)re.child.return=re,re=re.child;else{if(re===L)break e;for(;re.sibling===null;){if(re.return===null||re.return===L)break e;re=re.return}re.sibling.return=re.return,re=re.sibling}m?(h=c,L=r.stateNode,h.nodeType===8?h.parentNode.removeChild(L):h.removeChild(L)):c.removeChild(r.stateNode)}else if(r.tag===4){if(r.child!==null){c=r.stateNode.containerInfo,m=!0,r.child.return=r,r=r.child;continue}}else if(lu(e,r,n),r.child!==null){r.child.return=r,r=r.child;continue}if(r===t)break;for(;r.sibling===null;){if(r.return===null||r.return===t)return;r=r.return,r.tag===4&&(s=!1)}r.sibling.return=r.return,r=r.sibling}}i(cu,"Mi");function Gs(e,t){switch(t.tag){case 0:case 11:case 14:case 15:case 22:ou(3,t);return;case 1:return;case 5:var n=t.stateNode;if(n!=null){var r=t.memoizedProps,s=e!==null?e.memoizedProps:r;e=t.type;var c=t.updateQueue;if(t.updateQueue=null,c!==null){for(n[Nr]=r,e==="input"&&r.type==="radio"&&r.name!=null&&fi(n,r),yo(e,s),t=yo(e,r),s=0;s<c.length;s+=2){var m=c[s],h=c[s+1];m==="style"?Ni(n,h):m==="dangerouslySetInnerHTML"?gi(n,h):m==="children"?Dn(n,h):Br(n,m,h,t)}switch(e){case"input":Qr(n,r);break;case"textarea":pi(n,r);break;case"select":t=n._wrapperState.wasMultiple,n._wrapperState.wasMultiple=!!r.multiple,e=r.value,e!=null?Ue(n,!!r.multiple,e,!1):t!==!!r.multiple&&(r.defaultValue!=null?Ue(n,!!r.multiple,r.defaultValue,!0):Ue(n,!!r.multiple,r.multiple?[]:"",!1))}}}return;case 6:if(t.stateNode===null)throw Error(v(162));t.stateNode.nodeValue=t.memoizedProps;return;case 3:t=t.stateNode,t.hydrate&&(t.hydrate=!1,Li(t.containerInfo));return;case 12:return;case 13:if(n=t,t.memoizedState===null?r=!1:(r=!0,n=t.child,ea=Ct()),n!==null)e:for(e=n;;){if(e.tag===5)c=e.stateNode,r?(c=c.style,typeof c.setProperty=="function"?c.setProperty("display","none","important"):c.display="none"):(c=e.stateNode,s=e.memoizedProps.style,s=s!=null&&s.hasOwnProperty("display")?s.display:null,c.style.display=vo("display",s));else if(e.tag===6)e.stateNode.nodeValue=r?"":e.memoizedProps;else if(e.tag===13&&e.memoizedState!==null&&e.memoizedState.dehydrated===null){c=e.child.sibling,c.return=e,e=c;continue}else if(e.child!==null){e.child.return=e,e=e.child;continue}if(e===n)break;for(;e.sibling===null;){if(e.return===null||e.return===n)break e;e=e.return}e.sibling.return=e.return,e=e.sibling}du(t);return;case 19:du(t);return;case 17:return}throw Error(v(163))}i(Gs,"Si");function du(e){var t=e.updateQueue;if(t!==null){e.updateQueue=null;var n=e.stateNode;n===null&&(n=e.stateNode=new Hu),t.forEach(function(r){var s=Yu.bind(null,e,r);n.has(r)||(n.add(r),r.then(s,s))})}}i(du,"Ui");var $u=typeof WeakMap=="function"?WeakMap:Map;function fu(e,t,n){n=_n(n,null),n.tag=3,n.payload={element:null};var r=t.value;return n.callback=function(){Sl||(Sl=!0,ta=r),Qs(e,t)},n}i(fu,"Xi");function mu(e,t,n){n=_n(n,null),n.tag=3;var r=e.type.getDerivedStateFromError;if(typeof r=="function"){var s=t.value;n.payload=function(){return Qs(e,t),r(s)}}var c=e.stateNode;return c!==null&&typeof c.componentDidCatch=="function"&&(n.callback=function(){typeof r!="function"&&(Nn===null?Nn=new Set([this]):Nn.add(this),Qs(e,t));var m=t.stack;this.componentDidCatch(t.value,{componentStack:m!==null?m:""})}),n}i(mu,"$i");var ju=Math.ceil,wl=xt.ReactCurrentDispatcher,pu=xt.ReactCurrentOwner,it=0,Xs=8,$t=16,Gt=32,or=0,xl=1,hu=2,El=3,kl=4,Js=5,xe=it,_t=null,_e=null,wt=0,lt=or,bl=null,un=1073741823,ei=1073741823,_l=null,ti=0,Ll=!1,ea=0,vu=500,pe=null,Sl=!1,ta=null,Nn=null,Tl=!1,ni=null,ri=90,ir=null,oi=0,na=null,Nl=0;function Xt(){return(xe&($t|Gt))!==it?1073741821-(Ct()/10|0):Nl!==0?Nl:Nl=1073741821-(Ct()/10|0)}i(Xt,"Gg");function lr(e,t,n){if(t=t.mode,!(t&2))return 1073741823;var r=tl();if(!(t&4))return r===99?1073741823:1073741822;if((xe&$t)!==it)return wt;if(n!==null)e=nl(e,n.timeoutMs|0||5e3,250);else switch(r){case 99:e=1073741823;break;case 98:e=nl(e,150,100);break;case 97:case 96:e=nl(e,5e3,250);break;case 95:e=2;break;default:throw Error(v(326))}return _t!==null&&e===wt&&--e,e}i(lr,"Hg");function Mn(e,t){if(50<oi)throw oi=0,na=null,Error(v(185));if(e=Ml(e,t),e!==null){var n=tl();t===1073741823?(xe&Xs)!==it&&(xe&($t|Gt))===it?ra(e):(Lt(e),xe===it&&Qt()):Lt(e),(xe&4)===it||n!==98&&n!==99||(ir===null?ir=new Map([[e,t]]):(n=ir.get(e),(n===void 0||n>t)&&ir.set(e,t)))}}i(Mn,"Ig");function Ml(e,t){e.expirationTime<t&&(e.expirationTime=t);var n=e.alternate;n!==null&&n.expirationTime<t&&(n.expirationTime=t);var r=e.return,s=null;if(r===null&&e.tag===3)s=e.stateNode;else for(;r!==null;){if(n=r.alternate,r.childExpirationTime<t&&(r.childExpirationTime=t),n!==null&&n.childExpirationTime<t&&(n.childExpirationTime=t),r.return===null&&r.tag===3){s=r.stateNode;break}r=r.return}return s!==null&&(_t===s&&(Pl(t),lt===kl&&dr(s,wt)),Tu(s,t)),s}i(Ml,"xj");function Rl(e){var t=e.lastExpiredTime;if(t!==0||(t=e.firstPendingTime,!Su(e,t)))return t;var n=e.lastPingedTime;return e=e.nextKnownPendingLevel,e=n>e?n:e,2>=e&&t!==e?0:e}i(Rl,"zj");function Lt(e){if(e.lastExpiredTime!==0)e.callbackExpirationTime=1073741823,e.callbackPriority=99,e.callbackNode=ka(ra.bind(null,e));else{var t=Rl(e),n=e.callbackNode;if(t===0)n!==null&&(e.callbackNode=null,e.callbackExpirationTime=0,e.callbackPriority=90);else{var r=Xt();if(t===1073741823?r=99:t===1||t===2?r=95:(r=10*(1073741821-t)-10*(1073741821-r),r=0>=r?99:250>=r?98:5250>=r?97:95),n!==null){var s=e.callbackPriority;if(e.callbackExpirationTime===t&&s>=r)return;n!==tt&&ne(n)}e.callbackExpirationTime=t,e.callbackPriority=r,t=t===1073741823?ka(ra.bind(null,e)):Ea(r,gu.bind(null,e),{timeout:10*(1073741821-t)-Ct()}),e.callbackNode=t}}}i(Lt,"Z");function gu(e,t){if(Nl=0,t)return t=Xt(),ca(e,t),Lt(e),null;var n=Rl(e);if(n!==0){if(t=e.callbackNode,(xe&($t|Gt))!==it)throw Error(v(327));if($r(),e===_t&&n===wt||sr(e,n),_e!==null){var r=xe;xe|=$t;var s=xu();do try{Wu();break}catch(h){wu(e,h)}while(!0);if(ks(),xe=r,wl.current=s,lt===xl)throw t=bl,sr(e,n),dr(e,n),Lt(e),t;if(_e===null)switch(s=e.finishedWork=e.current.alternate,e.finishedExpirationTime=n,r=lt,_t=null,r){case or:case xl:throw Error(v(345));case hu:ca(e,2<n?2:n);break;case El:if(dr(e,n),r=e.lastSuspendedTime,n===r&&(e.nextKnownPendingLevel=oa(s)),un===1073741823&&(s=ea+vu-Ct(),10<s)){if(Ll){var c=e.lastPingedTime;if(c===0||c>=n){e.lastPingedTime=n,sr(e,n);break}}if(c=Rl(e),c!==0&&c!==n)break;if(r!==0&&r!==n){e.lastPingedTime=r;break}e.timeoutHandle=Bn(ar.bind(null,e),s);break}ar(e);break;case kl:if(dr(e,n),r=e.lastSuspendedTime,n===r&&(e.nextKnownPendingLevel=oa(s)),Ll&&(s=e.lastPingedTime,s===0||s>=n)){e.lastPingedTime=n,sr(e,n);break}if(s=Rl(e),s!==0&&s!==n)break;if(r!==0&&r!==n){e.lastPingedTime=r;break}if(ei!==1073741823?r=10*(1073741821-ei)-Ct():un===1073741823?r=0:(r=10*(1073741821-un)-5e3,s=Ct(),n=10*(1073741821-n)-s,r=s-r,0>r&&(r=0),r=(120>r?120:480>r?480:1080>r?1080:1920>r?1920:3e3>r?3e3:4320>r?4320:1960*ju(r/1960))-r,n<r&&(r=n)),10<r){e.timeoutHandle=Bn(ar.bind(null,e),r);break}ar(e);break;case Js:if(un!==1073741823&&_l!==null){c=un;var m=_l;if(r=m.busyMinDurationMs|0,0>=r?r=0:(s=m.busyDelayMs|0,c=Ct()-(10*(1073741821-c)-(m.timeoutMs|0||5e3)),r=c<=s?0:s+r-c),10<r){dr(e,n),e.timeoutHandle=Bn(ar.bind(null,e),r);break}}ar(e);break;default:throw Error(v(329))}if(Lt(e),e.callbackNode===t)return gu.bind(null,e)}}return null}i(gu,"Bj");function ra(e){var t=e.lastExpiredTime;if(t=t!==0?t:1073741823,(xe&($t|Gt))!==it)throw Error(v(327));if($r(),e===_t&&t===wt||sr(e,t),_e!==null){var n=xe;xe|=$t;var r=xu();do try{Uu();break}catch(s){wu(e,s)}while(!0);if(ks(),xe=n,wl.current=r,lt===xl)throw n=bl,sr(e,t),dr(e,t),Lt(e),n;if(_e!==null)throw Error(v(261));e.finishedWork=e.current.alternate,e.finishedExpirationTime=t,_t=null,ar(e),Lt(e)}return null}i(ra,"yj");function Bu(){if(ir!==null){var e=ir;ir=null,e.forEach(function(t,n){ca(n,t),Lt(n)}),Qt()}}i(Bu,"Lj");function yu(e,t){var n=xe;xe|=1;try{return e(t)}finally{xe=n,xe===it&&Qt()}}i(yu,"Mj");function Cu(e,t){var n=xe;xe&=-2,xe|=Xs;try{return e(t)}finally{xe=n,xe===it&&Qt()}}i(Cu,"Nj");function sr(e,t){e.finishedWork=null,e.finishedExpirationTime=0;var n=e.timeoutHandle;if(n!==-1&&(e.timeoutHandle=-1,Xl(n)),_e!==null)for(n=_e.return;n!==null;){var r=n;switch(r.tag){case 1:r=r.type.childContextTypes,r!=null&&p();break;case 3:Fr(),Ve(a),Ve(o);break;case 5:Ms(r);break;case 4:Fr();break;case 13:Ve(Ge);break;case 19:Ve(Ge);break;case 10:bs(r)}n=n.return}_t=e,_e=cr(e.current,null),wt=t,lt=or,bl=null,ei=un=1073741823,_l=null,ti=0,Ll=!1}i(sr,"Ej");function wu(e,t){do{try{if(ks(),dl.current=gl,fl)for(var n=rt.memoizedState;n!==null;){var r=n.queue;r!==null&&(r.pending=null),n=n.next}if(Sn=0,mt=ft=rt=null,fl=!1,_e===null||_e.return===null)return lt=xl,bl=t,_e=null;e:{var s=e,c=_e.return,m=_e,h=t;if(t=wt,m.effectTag|=2048,m.firstEffect=m.lastEffect=null,h!==null&&typeof h=="object"&&typeof h.then=="function"){var L=h;if(!(m.mode&2)){var S=m.alternate;S?(m.updateQueue=S.updateQueue,m.memoizedState=S.memoizedState,m.expirationTime=S.expirationTime):(m.updateQueue=null,m.memoizedState=null)}var re=(Ge.current&1)!==0,ue=c;do{var Me;if(Me=ue.tag===13){var Ae=ue.memoizedState;if(Ae!==null)Me=Ae.dehydrated!==null;else{var Rt=ue.memoizedProps;Me=Rt.fallback===void 0?!1:Rt.unstable_avoidThisFallback!==!0?!0:!re}}if(Me){var ct=ue.updateQueue;if(ct===null){var b=new Set;b.add(L),ue.updateQueue=b}else ct.add(L);if(!(ue.mode&2)){if(ue.effectTag|=64,m.effectTag&=-2981,m.tag===1)if(m.alternate===null)m.tag=17;else{var x=_n(1073741823,null);x.tag=2,Ln(m,x)}m.expirationTime=1073741823;break e}h=void 0,m=t;var N=s.pingCache;if(N===null?(N=s.pingCache=new $u,h=new Set,N.set(L,h)):(h=N.get(L),h===void 0&&(h=new Set,N.set(L,h))),!h.has(m)){h.add(m);var j=Ku.bind(null,s,L,m);L.then(j,j)}ue.effectTag|=4096,ue.expirationTime=t;break e}ue=ue.return}while(ue!==null);h=Error((jt(m.type)||"A React component")+` suspended while rendering, but no fallback UI was specified.

Add a <Suspense fallback=...> component higher in the tree to provide a loading indicator or placeholder to display.`+ci(m))}lt!==Js&&(lt=hu),h=Zs(h,m),ue=c;do{switch(ue.tag){case 3:L=h,ue.effectTag|=4096,ue.expirationTime=t;var X=fu(ue,L,t);La(ue,X);break e;case 1:L=h;var ce=ue.type,ye=ue.stateNode;if(!(ue.effectTag&64)&&(typeof ce.getDerivedStateFromError=="function"||ye!==null&&typeof ye.componentDidCatch=="function"&&(Nn===null||!Nn.has(ye)))){ue.effectTag|=4096,ue.expirationTime=t;var Pe=mu(ue,L,t);La(ue,Pe);break e}}ue=ue.return}while(ue!==null)}_e=bu(_e)}catch(Ye){t=Ye;continue}break}while(!0)}i(wu,"Hj");function xu(){var e=wl.current;return wl.current=gl,e===null?gl:e}i(xu,"Fj");function Eu(e,t){e<un&&2<e&&(un=e),t!==null&&e<ei&&2<e&&(ei=e,_l=t)}i(Eu,"Ag");function Pl(e){e>ti&&(ti=e)}i(Pl,"Bg");function Uu(){for(;_e!==null;)_e=ku(_e)}i(Uu,"Kj");function Wu(){for(;_e!==null&&!Ne();)_e=ku(_e)}i(Wu,"Gj");function ku(e){var t=Lu(e.alternate,e,wt);return e.memoizedProps=e.pendingProps,t===null&&(t=bu(e)),pu.current=null,t}i(ku,"Qj");function bu(e){_e=e;do{var t=_e.alternate;if(e=_e.return,_e.effectTag&2048){if(t=Iu(_e),t!==null)return t.effectTag&=2047,t;e!==null&&(e.firstEffect=e.lastEffect=null,e.effectTag|=2048)}else{if(t=Au(t,_e,wt),wt===1||_e.childExpirationTime!==1){for(var n=0,r=_e.child;r!==null;){var s=r.expirationTime,c=r.childExpirationTime;s>n&&(n=s),c>n&&(n=c),r=r.sibling}_e.childExpirationTime=n}if(t!==null)return t;e!==null&&!(e.effectTag&2048)&&(e.firstEffect===null&&(e.firstEffect=_e.firstEffect),_e.lastEffect!==null&&(e.lastEffect!==null&&(e.lastEffect.nextEffect=_e.firstEffect),e.lastEffect=_e.lastEffect),1<_e.effectTag&&(e.lastEffect!==null?e.lastEffect.nextEffect=_e:e.firstEffect=_e,e.lastEffect=_e))}if(t=_e.sibling,t!==null)return t;_e=e}while(_e!==null);return lt===or&&(lt=Js),null}i(bu,"Pj");function oa(e){var t=e.expirationTime;return e=e.childExpirationTime,t>e?t:e}i(oa,"Ij");function ar(e){var t=tl();return kn(99,qu.bind(null,e,t)),null}i(ar,"Jj");function qu(e,t){do $r();while(ni!==null);if((xe&($t|Gt))!==it)throw Error(v(327));var n=e.finishedWork,r=e.finishedExpirationTime;if(n===null)return null;if(e.finishedWork=null,e.finishedExpirationTime=0,n===e.current)throw Error(v(177));e.callbackNode=null,e.callbackExpirationTime=0,e.callbackPriority=90,e.nextKnownPendingLevel=0;var s=oa(n);if(e.firstPendingTime=s,r<=e.lastSuspendedTime?e.firstSuspendedTime=e.lastSuspendedTime=e.nextKnownPendingLevel=0:r<=e.firstSuspendedTime&&(e.firstSuspendedTime=r-1),r<=e.lastPingedTime&&(e.lastPingedTime=0),r<=e.lastExpiredTime&&(e.lastExpiredTime=0),e===_t&&(_e=_t=null,wt=0),1<n.effectTag?n.lastEffect!==null?(n.lastEffect.nextEffect=n,s=n.firstEffect):s=n:s=n.firstEffect,s!==null){var c=xe;xe|=Gt,pu.current=null,bo=Lr;var m=Oi();if(xo(m)){if("selectionStart"in m)var h={start:m.selectionStart,end:m.selectionEnd};else e:{h=(h=m.ownerDocument)&&h.defaultView||window;var L=h.getSelection&&h.getSelection();if(L&&L.rangeCount!==0){h=L.anchorNode;var S=L.anchorOffset,re=L.focusNode;L=L.focusOffset;try{h.nodeType,re.nodeType}catch{h=null;break e}var ue=0,Me=-1,Ae=-1,Rt=0,ct=0,b=m,x=null;t:for(;;){for(var N;b!==h||S!==0&&b.nodeType!==3||(Me=ue+S),b!==re||L!==0&&b.nodeType!==3||(Ae=ue+L),b.nodeType===3&&(ue+=b.nodeValue.length),(N=b.firstChild)!==null;)x=b,b=N;for(;;){if(b===m)break t;if(x===h&&++Rt===S&&(Me=ue),x===re&&++ct===L&&(Ae=ue),(N=b.nextSibling)!==null)break;b=x,x=b.parentNode}b=N}h=Me===-1||Ae===-1?null:{start:Me,end:Ae}}else h=null}h=h||{start:0,end:0}}else h=null;_o={activeElementDetached:null,focusedElem:m,selectionRange:h},Lr=!1,pe=s;do try{Zu()}catch(Le){if(pe===null)throw Error(v(330));ur(pe,Le),pe=pe.nextEffect}while(pe!==null);pe=s;do try{for(m=e,h=t;pe!==null;){var j=pe.effectTag;if(j&16&&Dn(pe.stateNode,""),j&128){var X=pe.alternate;if(X!==null){var ce=X.ref;ce!==null&&(typeof ce=="function"?ce(null):ce.current=null)}}switch(j&1038){case 2:uu(pe),pe.effectTag&=-3;break;case 6:uu(pe),pe.effectTag&=-3,Gs(pe.alternate,pe);break;case 1024:pe.effectTag&=-1025;break;case 1028:pe.effectTag&=-1025,Gs(pe.alternate,pe);break;case 4:Gs(pe.alternate,pe);break;case 8:S=pe,cu(m,S,h),su(S)}pe=pe.nextEffect}}catch(Le){if(pe===null)throw Error(v(330));ur(pe,Le),pe=pe.nextEffect}while(pe!==null);if(ce=_o,X=Oi(),j=ce.focusedElem,h=ce.selectionRange,X!==j&&j&&j.ownerDocument&&Pi(j.ownerDocument.documentElement,j)){for(h!==null&&xo(j)&&(X=h.start,ce=h.end,ce===void 0&&(ce=X),"selectionStart"in j?(j.selectionStart=X,j.selectionEnd=Math.min(ce,j.value.length)):(ce=(X=j.ownerDocument||document)&&X.defaultView||window,ce.getSelection&&(ce=ce.getSelection(),S=j.textContent.length,m=Math.min(h.start,S),h=h.end===void 0?m:Math.min(h.end,S),!ce.extend&&m>h&&(S=h,h=m,m=S),S=wo(j,m),re=wo(j,h),S&&re&&(ce.rangeCount!==1||ce.anchorNode!==S.node||ce.anchorOffset!==S.offset||ce.focusNode!==re.node||ce.focusOffset!==re.offset)&&(X=X.createRange(),X.setStart(S.node,S.offset),ce.removeAllRanges(),m>h?(ce.addRange(X),ce.extend(re.node,re.offset)):(X.setEnd(re.node,re.offset),ce.addRange(X)))))),X=[],ce=j;ce=ce.parentNode;)ce.nodeType===1&&X.push({element:ce,left:ce.scrollLeft,top:ce.scrollTop});for(typeof j.focus=="function"&&j.focus(),j=0;j<X.length;j++)ce=X[j],ce.element.scrollLeft=ce.left,ce.element.scrollTop=ce.top}Lr=!!bo,_o=bo=null,e.current=n,pe=s;do try{for(j=e;pe!==null;){var ye=pe.effectTag;if(ye&36&&Vu(j,pe.alternate,pe),ye&128){X=void 0;var Pe=pe.ref;if(Pe!==null){var Ye=pe.stateNode;switch(pe.tag){case 5:X=Ye;break;default:X=Ye}typeof Pe=="function"?Pe(X):Pe.current=X}}pe=pe.nextEffect}}catch(Le){if(pe===null)throw Error(v(330));ur(pe,Le),pe=pe.nextEffect}while(pe!==null);pe=null,Zt(),xe=c}else e.current=n;if(Tl)Tl=!1,ni=e,ri=t;else for(pe=s;pe!==null;)t=pe.nextEffect,pe.nextEffect=null,pe=t;if(t=e.firstPendingTime,t===0&&(Nn=null),t===1073741823?e===na?oi++:(oi=0,na=e):oi=0,typeof ia=="function"&&ia(n.stateNode,r),Lt(e),Sl)throw Sl=!1,e=ta,ta=null,e;return(xe&Xs)!==it||Qt(),null}i(qu,"Sj");function Zu(){for(;pe!==null;){var e=pe.effectTag;e&256&&zu(pe.alternate,pe),!(e&512)||Tl||(Tl=!0,Ea(97,function(){return $r(),null})),pe=pe.nextEffect}}i(Zu,"Tj");function $r(){if(ri!==90){var e=97<ri?97:ri;return ri=90,kn(e,Qu)}}i($r,"Dj");function Qu(){if(ni===null)return!1;var e=ni;if(ni=null,(xe&($t|Gt))!==it)throw Error(v(331));var t=xe;for(xe|=Gt,e=e.current.firstEffect;e!==null;){try{var n=e;if(n.effectTag&512)switch(n.tag){case 0:case 11:case 15:case 22:ou(5,n),iu(5,n)}}catch(r){if(e===null)throw Error(v(330));ur(e,r)}n=e.nextEffect,e.nextEffect=null,e=n}return xe=t,Qt(),!0}i(Qu,"Vj");function _u(e,t,n){t=Zs(n,t),t=fu(e,t,1073741823),Ln(e,t),e=Ml(e,1073741823),e!==null&&Lt(e)}i(_u,"Wj");function ur(e,t){if(e.tag===3)_u(e,e,t);else for(var n=e.return;n!==null;){if(n.tag===3){_u(n,e,t);break}else if(n.tag===1){var r=n.stateNode;if(typeof n.type.getDerivedStateFromError=="function"||typeof r.componentDidCatch=="function"&&(Nn===null||!Nn.has(r))){e=Zs(t,e),e=mu(n,e,1073741823),Ln(n,e),n=Ml(n,1073741823),n!==null&&Lt(n);break}}n=n.return}}i(ur,"Ei");function Ku(e,t,n){var r=e.pingCache;r!==null&&r.delete(t),_t===e&&wt===n?lt===kl||lt===El&&un===1073741823&&Ct()-ea<vu?sr(e,wt):Ll=!0:Su(e,n)&&(t=e.lastPingedTime,t!==0&&t<n||(e.lastPingedTime=n,Lt(e)))}i(Ku,"Oj");function Yu(e,t){var n=e.stateNode;n!==null&&n.delete(t),t=0,t===0&&(t=Xt(),t=lr(t,e,null)),e=Ml(e,t),e!==null&&Lt(e)}i(Yu,"Vi");var Lu;Lu=i(function(e,t,n){var r=t.expirationTime;if(e!==null){var s=t.pendingProps;if(e.memoizedProps!==s||a.current)Yt=!0;else{if(r<n){switch(Yt=!1,t.tag){case 3:Ya(t),$s();break;case 5:if(Oa(t),t.mode&4&&n!==1&&s.hidden)return t.expirationTime=t.childExpirationTime=1,null;break;case 1:f(t.type)&&k(t);break;case 4:Ns(t,t.stateNode.containerInfo);break;case 10:r=t.memoizedProps.value,s=t.type._context,We(rl,s._currentValue),s._currentValue=r;break;case 13:if(t.memoizedState!==null)return r=t.child.childExpirationTime,r!==0&&r>=n?Ga(e,t,n):(We(Ge,Ge.current&1),t=an(e,t,n),t!==null?t.sibling:null);We(Ge,Ge.current&1);break;case 19:if(r=t.childExpirationTime>=n,e.effectTag&64){if(r)return Ja(e,t,n);t.effectTag|=64}if(s=t.memoizedState,s!==null&&(s.rendering=null,s.tail=null),We(Ge,Ge.current),!r)return null}return an(e,t,n)}Yt=!1}}else Yt=!1;switch(t.expirationTime=0,t.tag){case 2:if(r=t.type,e!==null&&(e.alternate=null,t.alternate=null,t.effectTag|=2),e=t.pendingProps,s=d(t,o.current),Ir(t,n),s=Os(null,t,r,e,s,n),t.effectTag|=1,typeof s=="object"&&s!==null&&typeof s.render=="function"&&s.$$typeof===void 0){if(t.tag=1,t.memoizedState=null,t.updateQueue=null,f(r)){var c=!0;k(t)}else c=!1;t.memoizedState=s.state!==null&&s.state!==void 0?s.state:null,_s(t);var m=r.getDerivedStateFromProps;typeof m=="function"&&ll(t,r,m,e),s.updater=sl,t.stateNode=s,s._reactInternalFiber=t,Ss(t,r,e,n),t=Bs(null,t,r,!0,c,n)}else t.tag=0,bt(null,t,s,n),t=t.child;return t;case 16:e:{if(s=t.elementType,e!==null&&(e.alternate=null,t.alternate=null,t.effectTag|=2),e=t.pendingProps,pa(s),s._status!==1)throw s._result;switch(s=s._result,t.type=s,c=t.tag=Ju(s),e=Vt(s,e),c){case 0:t=js(null,t,s,e,n);break e;case 1:t=Ka(null,t,s,e,n);break e;case 11:t=Wa(null,t,s,e,n);break e;case 14:t=qa(null,t,s,Vt(s.type,e),r,n);break e}throw Error(v(306,s,""))}return t;case 0:return r=t.type,s=t.pendingProps,s=t.elementType===r?s:Vt(r,s),js(e,t,r,s,n);case 1:return r=t.type,s=t.pendingProps,s=t.elementType===r?s:Vt(r,s),Ka(e,t,r,s,n);case 3:if(Ya(t),r=t.updateQueue,e===null||r===null)throw Error(v(282));if(r=t.pendingProps,s=t.memoizedState,s=s!==null?s.element:null,Ls(e,t),Qo(t,r,null,n),r=t.memoizedState.element,r===s)$s(),t=an(e,t,n);else{if((s=t.stateNode.hydrate)&&(Tn=gn(t.stateNode.containerInfo.firstChild),sn=t,s=rr=!0),s)for(n=Ts(t,null,r,n),t.child=n;n;)n.effectTag=n.effectTag&-3|1024,n=n.sibling;else bt(e,t,r,n),$s();t=t.child}return t;case 5:return Oa(t),e===null&&Vs(t),r=t.type,s=t.pendingProps,c=e!==null?e.memoizedProps:null,m=s.children,Je(r,s)?m=null:c!==null&&Je(r,c)&&(t.effectTag|=16),Qa(e,t),t.mode&4&&n!==1&&s.hidden?(t.expirationTime=t.childExpirationTime=1,t=null):(bt(e,t,m,n),t=t.child),t;case 6:return e===null&&Vs(t),null;case 13:return Ga(e,t,n);case 4:return Ns(t,t.stateNode.containerInfo),r=t.pendingProps,e===null?t.child=Hr(t,null,r,n):bt(e,t,r,n),t.child;case 11:return r=t.type,s=t.pendingProps,s=t.elementType===r?s:Vt(r,s),Wa(e,t,r,s,n);case 7:return bt(e,t,t.pendingProps,n),t.child;case 8:return bt(e,t,t.pendingProps.children,n),t.child;case 12:return bt(e,t,t.pendingProps.children,n),t.child;case 10:e:{r=t.type._context,s=t.pendingProps,m=t.memoizedProps,c=s.value;var h=t.type._context;if(We(rl,h._currentValue),h._currentValue=c,m!==null)if(h=m.value,c=ln(h,c)?0:(typeof r._calculateChangedBits=="function"?r._calculateChangedBits(h,c):1073741823)|0,c===0){if(m.children===s.children&&!a.current){t=an(e,t,n);break e}}else for(h=t.child,h!==null&&(h.return=t);h!==null;){var L=h.dependencies;if(L!==null){m=h.child;for(var S=L.firstContext;S!==null;){if(S.context===r&&S.observedBits&c){h.tag===1&&(S=_n(n,null),S.tag=2,Ln(h,S)),h.expirationTime<n&&(h.expirationTime=n),S=h.alternate,S!==null&&S.expirationTime<n&&(S.expirationTime=n),_a(h.return,n),L.expirationTime<n&&(L.expirationTime=n);break}S=S.next}}else m=h.tag===10&&h.type===t.type?null:h.child;if(m!==null)m.return=h;else for(m=h;m!==null;){if(m===t){m=null;break}if(h=m.sibling,h!==null){h.return=m.return,m=h;break}m=m.return}h=m}bt(e,t,s.children,n),t=t.child}return t;case 9:return s=t.type,c=t.pendingProps,r=c.children,Ir(t,n),s=Nt(s,c.unstable_observedBits),r=r(s),t.effectTag|=1,bt(e,t,r,n),t.child;case 14:return s=t.type,c=Vt(s,t.pendingProps),c=Vt(s.type,c),qa(e,t,s,c,r,n);case 15:return Za(e,t,t.type,t.pendingProps,r,n);case 17:return r=t.type,s=t.pendingProps,s=t.elementType===r?s:Vt(r,s),e!==null&&(e.alternate=null,t.alternate=null,t.effectTag|=2),t.tag=1,f(r)?(e=!0,k(t)):e=!1,Ir(t,n),Ma(t,r,s),Ss(t,r,s,n),Bs(null,t,r,!0,e,n);case 19:return Ja(e,t,n)}throw Error(v(156,t.tag))},"Rj");var ia=null,la=null;function Gu(e){if(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__=="undefined")return!1;var t=__REACT_DEVTOOLS_GLOBAL_HOOK__;if(t.isDisabled||!t.supportsFiber)return!0;try{var n=t.inject(e);ia=i(function(r){try{t.onCommitFiberRoot(n,r,void 0,(r.current.effectTag&64)===64)}catch{}},"Uj"),la=i(function(r){try{t.onCommitFiberUnmount(n,r)}catch{}},"Li")}catch{}return!0}i(Gu,"Yj");function Xu(e,t,n,r){this.tag=e,this.key=n,this.sibling=this.child=this.return=this.stateNode=this.type=this.elementType=null,this.index=0,this.ref=null,this.pendingProps=t,this.dependencies=this.memoizedState=this.updateQueue=this.memoizedProps=null,this.mode=r,this.effectTag=0,this.lastEffect=this.firstEffect=this.nextEffect=null,this.childExpirationTime=this.expirationTime=0,this.alternate=null}i(Xu,"Zj");function Jt(e,t,n,r){return new Xu(e,t,n,r)}i(Jt,"Sh");function sa(e){return e=e.prototype,!(!e||!e.isReactComponent)}i(sa,"bi");function Ju(e){if(typeof e=="function")return sa(e)?1:0;if(e!=null){if(e=e.$$typeof,e===Wr)return 11;if(e===en)return 14}return 2}i(Ju,"Xj");function cr(e,t){var n=e.alternate;return n===null?(n=Jt(e.tag,t,e.key,e.mode),n.elementType=e.elementType,n.type=e.type,n.stateNode=e.stateNode,n.alternate=e,e.alternate=n):(n.pendingProps=t,n.effectTag=0,n.nextEffect=null,n.firstEffect=null,n.lastEffect=null),n.childExpirationTime=e.childExpirationTime,n.expirationTime=e.expirationTime,n.child=e.child,n.memoizedProps=e.memoizedProps,n.memoizedState=e.memoizedState,n.updateQueue=e.updateQueue,t=e.dependencies,n.dependencies=t===null?null:{expirationTime:t.expirationTime,firstContext:t.firstContext,responders:t.responders},n.sibling=e.sibling,n.index=e.index,n.ref=e.ref,n}i(cr,"Sg");function Ol(e,t,n,r,s,c){var m=2;if(r=e,typeof e=="function")sa(e)&&(m=1);else if(typeof e=="string")m=5;else e:switch(e){case Pt:return Rn(n.children,s,c,t);case zl:m=8,s|=7;break;case Fl:m=8,s|=1;break;case fr:return e=Jt(12,n,t,s|8),e.elementType=fr,e.type=fr,e.expirationTime=c,e;case dn:return e=Jt(13,n,t,s),e.type=dn,e.elementType=dn,e.expirationTime=c,e;case mr:return e=Jt(19,n,t,s),e.elementType=mr,e.expirationTime=c,e;default:if(typeof e=="object"&&e!==null)switch(e.$$typeof){case si:m=10;break e;case ai:m=9;break e;case Wr:m=11;break e;case en:m=14;break e;case Ot:m=16,r=null;break e;case ui:m=22;break e}throw Error(v(130,e==null?e:typeof e,""))}return t=Jt(m,n,t,s),t.elementType=e,t.type=r,t.expirationTime=c,t}i(Ol,"Ug");function Rn(e,t,n,r){return e=Jt(7,e,r,t),e.expirationTime=n,e}i(Rn,"Wg");function aa(e,t,n){return e=Jt(6,e,null,t),e.expirationTime=n,e}i(aa,"Tg");function ua(e,t,n){return t=Jt(4,e.children!==null?e.children:[],e.key,t),t.expirationTime=n,t.stateNode={containerInfo:e.containerInfo,pendingChildren:null,implementation:e.implementation},t}i(ua,"Vg");function ec(e,t,n){this.tag=t,this.current=null,this.containerInfo=e,this.pingCache=this.pendingChildren=null,this.finishedExpirationTime=0,this.finishedWork=null,this.timeoutHandle=-1,this.pendingContext=this.context=null,this.hydrate=n,this.callbackNode=null,this.callbackPriority=90,this.lastExpiredTime=this.lastPingedTime=this.nextKnownPendingLevel=this.lastSuspendedTime=this.firstSuspendedTime=this.firstPendingTime=0}i(ec,"ak");function Su(e,t){var n=e.firstSuspendedTime;return e=e.lastSuspendedTime,n!==0&&n>=t&&e<=t}i(Su,"Aj");function dr(e,t){var n=e.firstSuspendedTime,r=e.lastSuspendedTime;n<t&&(e.firstSuspendedTime=t),(r>t||n===0)&&(e.lastSuspendedTime=t),t<=e.lastPingedTime&&(e.lastPingedTime=0),t<=e.lastExpiredTime&&(e.lastExpiredTime=0)}i(dr,"xi");function Tu(e,t){t>e.firstPendingTime&&(e.firstPendingTime=t);var n=e.firstSuspendedTime;n!==0&&(t>=n?e.firstSuspendedTime=e.lastSuspendedTime=e.nextKnownPendingLevel=0:t>=e.lastSuspendedTime&&(e.lastSuspendedTime=t+1),t>e.nextKnownPendingLevel&&(e.nextKnownPendingLevel=t))}i(Tu,"yi");function ca(e,t){var n=e.lastExpiredTime;(n===0||n>t)&&(e.lastExpiredTime=t)}i(ca,"Cj");function Dl(e,t,n,r){var s=t.current,c=Xt(),m=Ko.suspense;c=lr(c,s,m);e:if(n){n=n._reactInternalFiber;t:{if(mn(n)!==n||n.tag!==1)throw Error(v(170));var h=n;do{switch(h.tag){case 3:h=h.stateNode.context;break t;case 1:if(f(h.type)){h=h.stateNode.__reactInternalMemoizedMergedChildContext;break t}}h=h.return}while(h!==null);throw Error(v(171))}if(n.tag===1){var L=n.type;if(f(L)){n=C(n,L,h);break e}}n=h}else n=Tt;return t.context===null?t.context=n:t.pendingContext=n,t=_n(c,m),t.payload={element:e},r=r===void 0?null:r,r!==null&&(t.callback=r),Ln(s,t),Mn(s,c),c}i(Dl,"bk");function da(e){if(e=e.current,!e.child)return null;switch(e.child.tag){case 5:return e.child.stateNode;default:return e.child.stateNode}}i(da,"ck");function Nu(e,t){e=e.memoizedState,e!==null&&e.dehydrated!==null&&e.retryTime<t&&(e.retryTime=t)}i(Nu,"dk");function fa(e,t){Nu(e,t),(e=e.alternate)&&Nu(e,t)}i(fa,"ek");function ma(e,t,n){n=n!=null&&n.hydrate===!0;var r=new ec(e,t,n),s=Jt(3,null,null,t===2?7:t===1?3:0);r.current=s,s.stateNode=r,_s(s),e[Un]=r.current,n&&t!==0&&hn(e,e.nodeType===9?e:e.ownerDocument),this._internalRoot=r}i(ma,"fk"),ma.prototype.render=function(e){Dl(e,this._internalRoot,null,null)},ma.prototype.unmount=function(){var e=this._internalRoot,t=e.containerInfo;Dl(null,e,null,function(){t[Un]=null})};function ii(e){return!(!e||e.nodeType!==1&&e.nodeType!==9&&e.nodeType!==11&&(e.nodeType!==8||e.nodeValue!==" react-mount-point-unstable "))}i(ii,"gk");function tc(e,t){if(t||(t=e?e.nodeType===9?e.documentElement:e.firstChild:null,t=!(!t||t.nodeType!==1||!t.hasAttribute("data-reactroot"))),!t)for(var n;n=e.lastChild;)e.removeChild(n);return new ma(e,0,t?{hydrate:!0}:void 0)}i(tc,"hk");function Al(e,t,n,r,s){var c=n._reactRootContainer;if(c){var m=c._internalRoot;if(typeof s=="function"){var h=s;s=i(function(){var S=da(m);h.call(S)},"e")}Dl(t,m,e,s)}else{if(c=n._reactRootContainer=tc(n,r),m=c._internalRoot,typeof s=="function"){var L=s;s=i(function(){var S=da(m);L.call(S)},"e")}Cu(function(){Dl(t,m,e,s)})}return da(m)}i(Al,"ik");function nc(e,t,n){var r=3<arguments.length&&arguments[3]!==void 0?arguments[3]:null;return{$$typeof:cn,key:r==null?null:""+r,children:e,containerInfo:t,implementation:n}}i(nc,"jk"),_i=i(function(e){if(e.tag===13){var t=nl(Xt(),150,100);Mn(e,t),fa(e,t)}},"wc"),xr=i(function(e){e.tag===13&&(Mn(e,3),fa(e,3))},"xc"),Er=i(function(e){if(e.tag===13){var t=Xt();t=lr(t,e,null),Mn(e,t),fa(e,t)}},"yc"),ge=i(function(e,t,n){switch(t){case"input":if(Qr(e,n),t=n.name,n.type==="radio"&&t!=null){for(n=e;n.parentNode;)n=n.parentNode;for(n=n.querySelectorAll("input[name="+JSON.stringify(""+t)+'][type="radio"]'),t=0;t<n.length;t++){var r=n[t];if(r!==e&&r.form===e.form){var s=So(r);if(!s)throw Error(v(90));jl(r),Qr(r,s)}}}break;case"textarea":pi(e,n);break;case"select":t=n.value,t!=null&&Ue(e,!!n.multiple,t,!1)}},"za"),Qe=yu,nt=i(function(e,t,n,r,s){var c=xe;xe|=4;try{return kn(98,e.bind(null,t,n,r,s))}finally{xe=c,xe===it&&Qt()}},"Ga"),st=i(function(){(xe&(1|$t|Gt))===it&&(Bu(),$r())},"Ha"),ot=i(function(e,t){var n=xe;xe|=2;try{return e(t)}finally{xe=n,xe===it&&Qt()}},"Ia");function Mu(e,t){var n=2<arguments.length&&arguments[2]!==void 0?arguments[2]:null;if(!ii(t))throw Error(v(200));return nc(e,t,null,n)}i(Mu,"kk");var rc={Events:[qn,rn,So,Q,E,yn,function(e){no(e,Jl)},Te,Ze,ho,ro,$r,{current:!1}]};(function(e){var t=e.findFiberByHostInstance;return Gu(I({},e,{overrideHookState:null,overrideProps:null,setSuspenseHandler:null,scheduleUpdate:null,currentDispatcherRef:xt.ReactCurrentDispatcher,findHostInstanceByFiber:i(function(n){return n=gr(n),n===null?null:n.stateNode},"findHostInstanceByFiber"),findFiberByHostInstance:i(function(n){return t?t(n):null},"findFiberByHostInstance"),findHostInstancesForRefresh:null,scheduleRefresh:null,scheduleRoot:null,setRefreshHandler:null,getCurrentFiber:null}))})({findFiberByHostInstance:Wn,bundleType:0,version:"16.14.0",rendererPackageName:"react-dom"}),oe=rc,oe=Mu,oe=i(function(e){if(e==null)return null;if(e.nodeType===1)return e;var t=e._reactInternalFiber;if(t===void 0)throw typeof e.render=="function"?Error(v(188)):Error(v(268,Object.keys(e)));return e=gr(t),e=e===null?null:e.stateNode,e},"__webpack_unused_export__"),oe=i(function(e,t){if((xe&($t|Gt))!==it)throw Error(v(187));var n=xe;xe|=1;try{return kn(99,e.bind(null,t))}finally{xe=n,Qt()}},"__webpack_unused_export__"),oe=i(function(e,t,n){if(!ii(t))throw Error(v(200));return Al(null,e,t,!0,n)},"__webpack_unused_export__"),R.render=function(e,t,n){if(!ii(t))throw Error(v(200));return Al(null,e,t,!1,n)},oe=i(function(e){if(!ii(e))throw Error(v(40));return e._reactRootContainer?(Cu(function(){Al(null,null,e,!1,function(){e._reactRootContainer=null,e[Un]=null})}),!0):!1},"__webpack_unused_export__"),oe=yu,oe=i(function(e,t){return Mu(e,t,2<arguments.length&&arguments[2]!==void 0?arguments[2]:null)},"__webpack_unused_export__"),oe=i(function(e,t,n,r){if(!ii(n))throw Error(v(200));if(e==null||e._reactInternalFiber===void 0)throw Error(v(38));return Al(e,t,n,!1,r)},"__webpack_unused_export__"),oe="16.14.0"},40961:(M,R,J)=>{"use strict";function oe(){if(!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__=="undefined"||typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE!="function"))try{__REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(oe)}catch(le){console.error(le)}}i(oe,"checkDCE"),oe(),M.exports=J(22551)},15287:(M,R,J)=>{"use strict";/** @license React v16.14.0
 * react.production.min.js
 *
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */var oe=J(45228),le=typeof Symbol=="function"&&Symbol.for,I=le?Symbol.for("react.element"):60103,y=le?Symbol.for("react.portal"):60106,v=le?Symbol.for("react.fragment"):60107,H=le?Symbol.for("react.strict_mode"):60108,z=le?Symbol.for("react.profiler"):60114,W=le?Symbol.for("react.provider"):60109,l=le?Symbol.for("react.context"):60110,ae=le?Symbol.for("react.forward_ref"):60112,G=le?Symbol.for("react.suspense"):60113,Oe=le?Symbol.for("react.memo"):60115,De=le?Symbol.for("react.lazy"):60116,$=typeof Symbol=="function"&&Symbol.iterator;function Z(w){for(var O="https://reactjs.org/docs/error-decoder.html?invariant="+w,he=1;he<arguments.length;he++)O+="&args[]="+encodeURIComponent(arguments[he]);return"Minified React error #"+w+"; visit "+O+" for the full message or use the non-minified dev environment for full errors and additional helpful warnings."}i(Z,"C");var me={isMounted:i(function(){return!1},"isMounted"),enqueueForceUpdate:i(function(){},"enqueueForceUpdate"),enqueueReplaceState:i(function(){},"enqueueReplaceState"),enqueueSetState:i(function(){},"enqueueSetState")},P={};function _(w,O,he){this.props=w,this.context=O,this.refs=P,this.updater=he||me}i(_,"F"),_.prototype.isReactComponent={},_.prototype.setState=function(w,O){if(typeof w!="object"&&typeof w!="function"&&w!=null)throw Error(Z(85));this.updater.enqueueSetState(this,w,O,"setState")},_.prototype.forceUpdate=function(w){this.updater.enqueueForceUpdate(this,w,"forceUpdate")};function T(){}i(T,"G"),T.prototype=_.prototype;function q(w,O,he){this.props=w,this.context=O,this.refs=P,this.updater=he||me}i(q,"H");var ee=q.prototype=new T;ee.constructor=q,oe(ee,_.prototype),ee.isPureReactComponent=!0;var V={current:null},E=Object.prototype.hasOwnProperty,A={key:!0,ref:!0,__self:!0,__source:!0};function ie(w,O,he){var Ee,we={},se=null,pt=null;if(O!=null)for(Ee in O.ref!==void 0&&(pt=O.ref),O.key!==void 0&&(se=""+O.key),O)E.call(O,Ee)&&!A.hasOwnProperty(Ee)&&(we[Ee]=O[Ee]);var ke=arguments.length-2;if(ke===1)we.children=he;else if(1<ke){for(var Se=Array(ke),ht=0;ht<ke;ht++)Se[ht]=arguments[ht+2];we.children=Se}if(w&&w.defaultProps)for(Ee in ke=w.defaultProps,ke)we[Ee]===void 0&&(we[Ee]=ke[Ee]);return{$$typeof:I,type:w,key:se,ref:pt,props:we,_owner:V.current}}i(ie,"M");function Q(w,O){return{$$typeof:I,type:w.type,key:O,ref:w.ref,props:w.props,_owner:w._owner}}i(Q,"N");function B(w){return typeof w=="object"&&w!==null&&w.$$typeof===I}i(B,"O");function ge(w){var O={"=":"=0",":":"=2"};return"$"+(""+w).replace(/[=:]/g,function(he){return O[he]})}i(ge,"escape");var ve=/\/+/g,de=[];function Ce(w,O,he,Ee){if(de.length){var we=de.pop();return we.result=w,we.keyPrefix=O,we.func=he,we.context=Ee,we.count=0,we}return{result:w,keyPrefix:O,func:he,context:Ee,count:0}}i(Ce,"R");function Te(w){w.result=null,w.keyPrefix=null,w.func=null,w.context=null,w.count=0,10>de.length&&de.push(w)}i(Te,"S");function Ze(w,O,he,Ee){var we=typeof w;(we==="undefined"||we==="boolean")&&(w=null);var se=!1;if(w===null)se=!0;else switch(we){case"string":case"number":se=!0;break;case"object":switch(w.$$typeof){case I:case y:se=!0}}if(se)return he(Ee,w,O===""?"."+nt(w,0):O),1;if(se=0,O=O===""?".":O+":",Array.isArray(w))for(var pt=0;pt<w.length;pt++){we=w[pt];var ke=O+nt(we,pt);se+=Ze(we,ke,he,Ee)}else if(w===null||typeof w!="object"?ke=null:(ke=$&&w[$]||w["@@iterator"],ke=typeof ke=="function"?ke:null),typeof ke=="function")for(w=ke.call(w),pt=0;!(we=w.next()).done;)we=we.value,ke=O+nt(we,pt++),se+=Ze(we,ke,he,Ee);else if(we==="object")throw he=""+w,Error(Z(31,he==="[object Object]"?"object with keys {"+Object.keys(w).join(", ")+"}":he,""));return se}i(Ze,"T");function Qe(w,O,he){return w==null?0:Ze(w,"",O,he)}i(Qe,"V");function nt(w,O){return typeof w=="object"&&w!==null&&w.key!=null?ge(w.key):O.toString(36)}i(nt,"U");function st(w,O){w.func.call(w.context,O,w.count++)}i(st,"W");function ot(w,O,he){var Ee=w.result,we=w.keyPrefix;w=w.func.call(w.context,O,w.count++),Array.isArray(w)?Fe(w,Ee,he,function(se){return se}):w!=null&&(B(w)&&(w=Q(w,we+(!w.key||O&&O.key===w.key?"":(""+w.key).replace(ve,"$&/")+"/")+he)),Ee.push(w))}i(ot,"aa");function Fe(w,O,he,Ee,we){var se="";he!=null&&(se=(""+he).replace(ve,"$&/")+"/"),O=Ce(O,se,Ee,we),Qe(w,ot,O),Te(O)}i(Fe,"X");var F={current:null};function U(){var w=F.current;if(w===null)throw Error(Z(321));return w}i(U,"Z");var te={ReactCurrentDispatcher:F,ReactCurrentBatchConfig:{suspense:null},ReactCurrentOwner:V,IsSomeRendererActing:{current:!1},assign:oe};R.Children={map:i(function(w,O,he){if(w==null)return w;var Ee=[];return Fe(w,Ee,null,O,he),Ee},"map"),forEach:i(function(w,O,he){if(w==null)return w;O=Ce(null,null,O,he),Qe(w,st,O),Te(O)},"forEach"),count:i(function(w){return Qe(w,function(){return null},null)},"count"),toArray:i(function(w){var O=[];return Fe(w,O,null,function(he){return he}),O},"toArray"),only:i(function(w){if(!B(w))throw Error(Z(143));return w},"only")},R.Component=_,R.Fragment=v,R.Profiler=z,R.PureComponent=q,R.StrictMode=H,R.Suspense=G,R.__SECRET_INTERNALS_DO_NOT_USE_OR_YOU_WILL_BE_FIRED=te,R.cloneElement=function(w,O,he){if(w==null)throw Error(Z(267,w));var Ee=oe({},w.props),we=w.key,se=w.ref,pt=w._owner;if(O!=null){if(O.ref!==void 0&&(se=O.ref,pt=V.current),O.key!==void 0&&(we=""+O.key),w.type&&w.type.defaultProps)var ke=w.type.defaultProps;for(Se in O)E.call(O,Se)&&!A.hasOwnProperty(Se)&&(Ee[Se]=O[Se]===void 0&&ke!==void 0?ke[Se]:O[Se])}var Se=arguments.length-2;if(Se===1)Ee.children=he;else if(1<Se){ke=Array(Se);for(var ht=0;ht<Se;ht++)ke[ht]=arguments[ht+2];Ee.children=ke}return{$$typeof:I,type:w.type,key:we,ref:se,props:Ee,_owner:pt}},R.createContext=function(w,O){return O===void 0&&(O=null),w={$$typeof:l,_calculateChangedBits:O,_currentValue:w,_currentValue2:w,_threadCount:0,Provider:null,Consumer:null},w.Provider={$$typeof:W,_context:w},w.Consumer=w},R.createElement=ie,R.createFactory=function(w){var O=ie.bind(null,w);return O.type=w,O},R.createRef=function(){return{current:null}},R.forwardRef=function(w){return{$$typeof:ae,render:w}},R.isValidElement=B,R.lazy=function(w){return{$$typeof:De,_ctor:w,_status:-1,_result:null}},R.memo=function(w,O){return{$$typeof:Oe,type:w,compare:O===void 0?null:O}},R.useCallback=function(w,O){return U().useCallback(w,O)},R.useContext=function(w,O){return U().useContext(w,O)},R.useDebugValue=function(){},R.useEffect=function(w,O){return U().useEffect(w,O)},R.useImperativeHandle=function(w,O,he){return U().useImperativeHandle(w,O,he)},R.useLayoutEffect=function(w,O){return U().useLayoutEffect(w,O)},R.useMemo=function(w,O){return U().useMemo(w,O)},R.useReducer=function(w,O,he){return U().useReducer(w,O,he)},R.useRef=function(w){return U().useRef(w)},R.useState=function(w){return U().useState(w)},R.version="16.14.0"},96540:(M,R,J)=>{"use strict";M.exports=J(15287)},7463:(M,R)=>{"use strict";/** @license React v0.19.1
 * scheduler.production.min.js
 *
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */var J,oe,le,I,y;if(typeof window=="undefined"||typeof MessageChannel!="function"){var v=null,H=null,z=i(function(){if(v!==null)try{var F=R.unstable_now();v(!0,F),v=null}catch(U){throw setTimeout(z,0),U}},"t"),W=Date.now();R.unstable_now=function(){return Date.now()-W},J=i(function(F){v!==null?setTimeout(J,0,F):(v=F,setTimeout(z,0))},"f"),oe=i(function(F,U){H=setTimeout(F,U)},"g"),le=i(function(){clearTimeout(H)},"h"),I=i(function(){return!1},"k"),y=R.unstable_forceFrameRate=function(){}}else{var l=window.performance,ae=window.Date,G=window.setTimeout,Oe=window.clearTimeout;if(typeof console!="undefined"){var De=window.cancelAnimationFrame;typeof window.requestAnimationFrame!="function"&&console.error("This browser doesn't support requestAnimationFrame. Make sure that you load a polyfill in older browsers. https://fb.me/react-polyfills"),typeof De!="function"&&console.error("This browser doesn't support cancelAnimationFrame. Make sure that you load a polyfill in older browsers. https://fb.me/react-polyfills")}if(typeof l=="object"&&typeof l.now=="function")R.unstable_now=function(){return l.now()};else{var $=ae.now();R.unstable_now=function(){return ae.now()-$}}var Z=!1,me=null,P=-1,_=5,T=0;I=i(function(){return R.unstable_now()>=T},"k"),y=i(function(){},"l"),R.unstable_forceFrameRate=function(F){0>F||125<F?console.error("forceFrameRate takes a positive int between 0 and 125, forcing framerates higher than 125 fps is not unsupported"):_=0<F?Math.floor(1e3/F):5};var q=new MessageChannel,ee=q.port2;q.port1.onmessage=function(){if(me!==null){var F=R.unstable_now();T=F+_;try{me(!0,F)?ee.postMessage(null):(Z=!1,me=null)}catch(U){throw ee.postMessage(null),U}}else Z=!1},J=i(function(F){me=F,Z||(Z=!0,ee.postMessage(null))},"f"),oe=i(function(F,U){P=G(function(){F(R.unstable_now())},U)},"g"),le=i(function(){Oe(P),P=-1},"h")}function V(F,U){var te=F.length;F.push(U);e:for(;;){var w=te-1>>>1,O=F[w];if(O!==void 0&&0<ie(O,U))F[w]=U,F[te]=O,te=w;else break e}}i(V,"J");function E(F){return F=F[0],F===void 0?null:F}i(E,"L");function A(F){var U=F[0];if(U!==void 0){var te=F.pop();if(te!==U){F[0]=te;e:for(var w=0,O=F.length;w<O;){var he=2*(w+1)-1,Ee=F[he],we=he+1,se=F[we];if(Ee!==void 0&&0>ie(Ee,te))se!==void 0&&0>ie(se,Ee)?(F[w]=se,F[we]=te,w=we):(F[w]=Ee,F[he]=te,w=he);else if(se!==void 0&&0>ie(se,te))F[w]=se,F[we]=te,w=we;else break e}}return U}return null}i(A,"M");function ie(F,U){var te=F.sortIndex-U.sortIndex;return te!==0?te:F.id-U.id}i(ie,"K");var Q=[],B=[],ge=1,ve=null,de=3,Ce=!1,Te=!1,Ze=!1;function Qe(F){for(var U=E(B);U!==null;){if(U.callback===null)A(B);else if(U.startTime<=F)A(B),U.sortIndex=U.expirationTime,V(Q,U);else break;U=E(B)}}i(Qe,"V");function nt(F){if(Ze=!1,Qe(F),!Te)if(E(Q)!==null)Te=!0,J(st);else{var U=E(B);U!==null&&oe(nt,U.startTime-F)}}i(nt,"W");function st(F,U){Te=!1,Ze&&(Ze=!1,le()),Ce=!0;var te=de;try{for(Qe(U),ve=E(Q);ve!==null&&(!(ve.expirationTime>U)||F&&!I());){var w=ve.callback;if(w!==null){ve.callback=null,de=ve.priorityLevel;var O=w(ve.expirationTime<=U);U=R.unstable_now(),typeof O=="function"?ve.callback=O:ve===E(Q)&&A(Q),Qe(U)}else A(Q);ve=E(Q)}if(ve!==null)var he=!0;else{var Ee=E(B);Ee!==null&&oe(nt,Ee.startTime-U),he=!1}return he}finally{ve=null,de=te,Ce=!1}}i(st,"X");function ot(F){switch(F){case 1:return-1;case 2:return 250;case 5:return 1073741823;case 4:return 1e4;default:return 5e3}}i(ot,"Y");var Fe=y;R.unstable_IdlePriority=5,R.unstable_ImmediatePriority=1,R.unstable_LowPriority=4,R.unstable_NormalPriority=3,R.unstable_Profiling=null,R.unstable_UserBlockingPriority=2,R.unstable_cancelCallback=function(F){F.callback=null},R.unstable_continueExecution=function(){Te||Ce||(Te=!0,J(st))},R.unstable_getCurrentPriorityLevel=function(){return de},R.unstable_getFirstCallbackNode=function(){return E(Q)},R.unstable_next=function(F){switch(de){case 1:case 2:case 3:var U=3;break;default:U=de}var te=de;de=U;try{return F()}finally{de=te}},R.unstable_pauseExecution=function(){},R.unstable_requestPaint=Fe,R.unstable_runWithPriority=function(F,U){switch(F){case 1:case 2:case 3:case 4:case 5:break;default:F=3}var te=de;de=F;try{return U()}finally{de=te}},R.unstable_scheduleCallback=function(F,U,te){var w=R.unstable_now();if(typeof te=="object"&&te!==null){var O=te.delay;O=typeof O=="number"&&0<O?w+O:w,te=typeof te.timeout=="number"?te.timeout:ot(F)}else te=ot(F),O=w;return te=O+te,F={id:ge++,callback:U,priorityLevel:F,startTime:O,expirationTime:te,sortIndex:-1},O>w?(F.sortIndex=O,V(B,F),E(Q)===null&&F===E(B)&&(Ze?le():Ze=!0,oe(nt,O-w))):(F.sortIndex=te,V(Q,F),Te||Ce||(Te=!0,J(st))),F},R.unstable_shouldYield=function(){var F=R.unstable_now();Qe(F);var U=E(Q);return U!==ve&&ve!==null&&U!==null&&U.callback!==null&&U.startTime<=F&&U.expirationTime<ve.expirationTime||I()},R.unstable_wrapCallback=function(F){var U=de;return function(){var te=de;de=U;try{return F.apply(this,arguments)}finally{de=te}}}},69982:(M,R,J)=>{"use strict";M.exports=J(7463)},85072:(M,R,J)=>{"use strict";var oe=i(function(){var Z;return i(function(){return typeof Z=="undefined"&&(Z=!!(window&&document&&document.all&&!window.atob)),Z},"memorize")},"isOldIE")(),le=i(function(){var Z={};return i(function(P){if(typeof Z[P]=="undefined"){var _=document.querySelector(P);if(window.HTMLIFrameElement&&_ instanceof window.HTMLIFrameElement)try{_=_.contentDocument.head}catch{_=null}Z[P]=_}return Z[P]},"memorize")},"getTarget")(),I=[];function y($){for(var Z=-1,me=0;me<I.length;me++)if(I[me].identifier===$){Z=me;break}return Z}i(y,"getIndexByIdentifier");function v($,Z){for(var me={},P=[],_=0;_<$.length;_++){var T=$[_],q=Z.base?T[0]+Z.base:T[0],ee=me[q]||0,V="".concat(q," ").concat(ee);me[q]=ee+1;var E=y(V),A={css:T[1],media:T[2],sourceMap:T[3]};E!==-1?(I[E].references++,I[E].updater(A)):I.push({identifier:V,updater:De(A,Z),references:1}),P.push(V)}return P}i(v,"modulesToDom");function H($){var Z=document.createElement("style"),me=$.attributes||{};if(typeof me.nonce=="undefined"){var P=J.nc;P&&(me.nonce=P)}if(Object.keys(me).forEach(function(T){Z.setAttribute(T,me[T])}),typeof $.insert=="function")$.insert(Z);else{var _=le($.insert||"head");if(!_)throw new Error("Couldn't find a style target. This probably means that the value for the 'insert' parameter is invalid.");_.appendChild(Z)}return Z}i(H,"insertStyleElement");function z($){if($.parentNode===null)return!1;$.parentNode.removeChild($)}i(z,"removeStyleElement");var W=i(function(){var Z=[];return i(function(P,_){return Z[P]=_,Z.filter(Boolean).join(`
`)},"replace")},"replaceText")();function l($,Z,me,P){var _=me?"":P.media?"@media ".concat(P.media," {").concat(P.css,"}"):P.css;if($.styleSheet)$.styleSheet.cssText=W(Z,_);else{var T=document.createTextNode(_),q=$.childNodes;q[Z]&&$.removeChild(q[Z]),q.length?$.insertBefore(T,q[Z]):$.appendChild(T)}}i(l,"applyToSingletonTag");function ae($,Z,me){var P=me.css,_=me.media,T=me.sourceMap;if(_?$.setAttribute("media",_):$.removeAttribute("media"),T&&typeof btoa!="undefined"&&(P+=`
/*# sourceMappingURL=data:application/json;base64,`.concat(btoa(unescape(encodeURIComponent(JSON.stringify(T))))," */")),$.styleSheet)$.styleSheet.cssText=P;else{for(;$.firstChild;)$.removeChild($.firstChild);$.appendChild(document.createTextNode(P))}}i(ae,"applyToTag");var G=null,Oe=0;function De($,Z){var me,P,_;if(Z.singleton){var T=Oe++;me=G||(G=H(Z)),P=l.bind(null,me,T,!1),_=l.bind(null,me,T,!0)}else me=H(Z),P=ae.bind(null,me,Z),_=i(function(){z(me)},"remove");return P($),i(function(ee){if(ee){if(ee.css===$.css&&ee.media===$.media&&ee.sourceMap===$.sourceMap)return;P($=ee)}else _()},"updateStyle")}i(De,"addStyle"),M.exports=function($,Z){Z=Z||{},!Z.singleton&&typeof Z.singleton!="boolean"&&(Z.singleton=oe()),$=$||[];var me=v($,Z);return i(function(_){if(_=_||[],Object.prototype.toString.call(_)==="[object Array]"){for(var T=0;T<me.length;T++){var q=me[T],ee=y(q);I[ee].references--}for(var V=v(_,Z),E=0;E<me.length;E++){var A=me[E],ie=y(A);I[ie].references===0&&(I[ie].updater(),I.splice(ie,1))}me=V}},"update")}},61440:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M14.12 13.9725L15 12.5L9.37924 2H7.61921L1.99847 12.5L2.87849 13.9725H14.12ZM2.87849 12.9725L8.49922 2.47249L14.12 12.9725H2.87849ZM7.98949 6H8.98799V10H7.98949V6ZM7.98949 11H8.98799V12H7.98949V11Z" fill="#C5C5C5"></path></svg>'},34439:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><g clip-path="url(#clip0_818_123307)"><path d="M16 7.99201C16 3.58042 12.416 0 8 0C3.584 0 0 3.58042 0 7.99201C0 10.4216 1.104 12.6114 2.832 14.0819C2.848 14.0979 2.864 14.0979 2.864 14.1139C3.008 14.2258 3.152 14.3377 3.312 14.4496C3.392 14.4975 3.456 14.5614 3.536 14.6254C4.816 15.4885 6.352 16 8.016 16C9.68 16 11.216 15.4885 12.496 14.6254C12.576 14.5774 12.64 14.5135 12.72 14.4655C12.864 14.3536 13.024 14.2418 13.168 14.1299C13.184 14.1139 13.2 14.1139 13.2 14.0979C14.896 12.6114 16 10.4216 16 7.99201ZM8 14.993C6.496 14.993 5.12 14.5135 3.984 13.7143C4 13.5864 4.032 13.4585 4.064 13.3307C4.16 12.979 4.304 12.6434 4.48 12.3397C4.656 12.036 4.864 11.7642 5.12 11.5245C5.36 11.2847 5.648 11.0609 5.936 10.8851C6.24 10.7093 6.56 10.5814 6.912 10.4855C7.264 10.3896 7.632 10.3417 8 10.3417C8.592 10.3417 9.136 10.4535 9.632 10.6613C10.128 10.8691 10.56 11.1568 10.928 11.5085C11.296 11.8761 11.584 12.3077 11.792 12.8032C11.904 13.0909 11.984 13.3946 12.032 13.7143C10.88 14.5135 9.504 14.993 8 14.993ZM5.552 7.59241C5.408 7.27273 5.344 6.92108 5.344 6.56943C5.344 6.21778 5.408 5.86613 5.552 5.54645C5.696 5.22677 5.888 4.93906 6.128 4.6993C6.368 4.45954 6.656 4.26773 6.976 4.12388C7.296 3.98002 7.648 3.91608 8 3.91608C8.368 3.91608 8.704 3.98002 9.024 4.12388C9.344 4.26773 9.632 4.45954 9.872 4.6993C10.112 4.93906 10.304 5.22677 10.448 5.54645C10.592 5.86613 10.656 6.21778 10.656 6.56943C10.656 6.93706 10.592 7.27273 10.448 7.59241C10.304 7.91209 10.112 8.1998 9.872 8.43956C9.632 8.67932 9.344 8.87113 9.024 9.01499C8.384 9.28671 7.6 9.28671 6.96 9.01499C6.64 8.87113 6.352 8.67932 6.112 8.43956C5.872 8.1998 5.68 7.91209 5.552 7.59241ZM12.976 12.8991C12.976 12.8671 12.96 12.8511 12.96 12.8192C12.8 12.3237 12.576 11.8442 12.272 11.4126C11.968 10.981 11.616 10.5974 11.184 10.2777C10.864 10.038 10.512 9.83017 10.144 9.67033C10.32 9.55844 10.48 9.41459 10.608 9.28671C10.848 9.04695 11.056 8.79121 11.232 8.5035C11.408 8.21578 11.536 7.91209 11.632 7.57642C11.728 7.24076 11.76 6.90509 11.76 6.56943C11.76 6.04196 11.664 5.54645 11.472 5.0989C11.28 4.65135 11.008 4.25175 10.656 3.9001C10.32 3.56444 9.904 3.29271 9.456 3.1009C9.008 2.90909 8.512 2.81319 7.984 2.81319C7.456 2.81319 6.96 2.90909 6.512 3.1009C6.064 3.29271 5.648 3.56444 5.312 3.91608C4.976 4.25175 4.704 4.66733 4.512 5.11489C4.32 5.56244 4.224 6.05794 4.224 6.58541C4.224 6.93706 4.272 7.27273 4.368 7.59241C4.464 7.92807 4.592 8.23177 4.768 8.51948C4.928 8.80719 5.152 9.06294 5.392 9.3027C5.536 9.44655 5.696 9.57443 5.872 9.68631C5.488 9.86214 5.136 10.0699 4.832 10.3097C4.416 10.6294 4.048 11.013 3.744 11.4286C3.44 11.8601 3.216 12.3237 3.056 12.8352C3.04 12.8671 3.04 12.8991 3.04 12.9151C1.776 11.6364 0.992 9.91009 0.992 7.99201C0.992 4.13986 4.144 0.991009 8 0.991009C11.856 0.991009 15.008 4.13986 15.008 7.99201C15.008 9.91009 14.224 11.6364 12.976 12.8991Z" fill="#C5C5C5"></path></g><defs><clipPath id="clip0_818_123307"><rect width="16" height="16" fill="white"></rect></clipPath></defs></svg>'},34894:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M13.78 4.22a.75.75 0 010 1.06l-7.25 7.25a.75.75 0 01-1.06 0L2.22 9.28a.75.75 0 011.06-1.06L6 10.94l6.72-6.72a.75.75 0 011.06 0z" fill="#C5C5C5"></path></svg>'},30407:M=>{M.exports='<svg viewBox="0 -2 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M7.97612 10.0719L12.3334 5.7146L12.9521 6.33332L8.28548 11L7.66676 11L3.0001 6.33332L3.61882 5.7146L7.97612 10.0719Z" fill="#C5C5C5"></path></svg>'},10650:M=>{M.exports='<svg viewBox="0 0 16 16" fill="currentColor" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M7.97612 10.0719L12.3334 5.7146L12.9521 6.33332L8.28548 11L7.66676 11L3.0001 6.33332L3.61882 5.7146L7.97612 10.0719Z"></path></svg>'},85130:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M7.99998 8.70711L11.6464 12.3536L12.3535 11.6464L8.70708 8L12.3535 4.35355L11.6464 3.64645L7.99998 7.29289L4.35353 3.64645L3.64642 4.35355L7.29287 8L3.64642 11.6464L4.35353 12.3536L7.99998 8.70711Z" fill="#C5C5C5"></path></svg>'},2301:M=>{M.exports='<svg viewBox="0 0 16 16" version="1.1" aria-hidden="true"><path fill-rule="evenodd" d="M14 1H2c-.55 0-1 .45-1 1v8c0 .55.45 1 1 1h2v3.5L7.5 11H14c.55 0 1-.45 1-1V2c0-.55-.45-1-1-1zm0 9H7l-2 2v-2H2V2h12v8z"></path></svg>'},5771:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M7.52 0H8.48V4.05333C9.47556 4.16 10.3111 4.58667 10.9867 5.33333C11.6622 6.08 12 6.96889 12 8C12 9.03111 11.6622 9.92 10.9867 10.6667C10.3111 11.4133 9.47556 11.84 8.48 11.9467V16H7.52V11.9467C6.52444 11.84 5.68889 11.4133 5.01333 10.6667C4.33778 9.92 4 9.03111 4 8C4 6.96889 4.33778 6.08 5.01333 5.33333C5.68889 4.58667 6.52444 4.16 7.52 4.05333V0ZM8 10.6133C8.71111 10.6133 9.31556 10.3644 9.81333 9.86667C10.3467 9.33333 10.6133 8.71111 10.6133 8C10.6133 7.28889 10.3467 6.68444 9.81333 6.18667C9.31556 5.65333 8.71111 5.38667 8 5.38667C7.28889 5.38667 6.66667 5.65333 6.13333 6.18667C5.63556 6.68444 5.38667 7.28889 5.38667 8C5.38667 8.71111 5.63556 9.33333 6.13333 9.86667C6.66667 10.3644 7.28889 10.6133 8 10.6133Z" fill="#A0A0A0"></path></svg>'},12158:M=>{M.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path d="M6.25 9.016C6.66421 9.016 7 9.35089 7 9.76399V11.26C7 11.6731 6.66421 12.008 6.25 12.008C5.83579 12.008 5.5 11.6731 5.5 11.26V9.76399C5.5 9.35089 5.83579 9.016 6.25 9.016Z"></path><path d="M10.5 9.76399C10.5 9.35089 10.1642 9.016 9.75 9.016C9.33579 9.016 9 9.35089 9 9.76399V11.26C9 11.6731 9.33579 12.008 9.75 12.008C10.1642 12.008 10.5 11.6731 10.5 11.26V9.76399Z"></path><path d="M7.86079 1.80482C7.91028 1.8577 7.95663 1.91232 8 1.96856C8.04337 1.91232 8.08972 1.8577 8.13921 1.80482C8.82116 1.07611 9.87702 0.90832 11.0828 1.04194C12.3131 1.17827 13.2283 1.56829 13.8072 2.29916C14.3725 3.01276 14.5 3.90895 14.5 4.77735C14.5 5.34785 14.447 5.92141 14.2459 6.428L14.4135 7.26391L14.4798 7.29699C15.4115 7.76158 16 8.71126 16 9.7501V11.0107C16 11.2495 15.9143 11.4478 15.844 11.5763C15.7691 11.7131 15.6751 11.8368 15.5851 11.9416C15.4049 12.1512 15.181 12.3534 14.9801 12.5202C14.7751 12.6907 14.5728 12.8419 14.4235 12.9494C14.1842 13.1217 13.9389 13.2807 13.6826 13.4277C13.3756 13.6038 12.9344 13.8361 12.3867 14.0679C11.2956 14.5296 9.75604 15 8 15C6.24396 15 4.70442 14.5296 3.61334 14.0679C3.06559 13.8361 2.62435 13.6038 2.31739 13.4277C2.0611 13.2807 1.81581 13.1217 1.57651 12.9494C1.42716 12.8419 1.2249 12.6907 1.01986 12.5202C0.819 12.3534 0.595113 12.1512 0.414932 11.9416C0.3249 11.8368 0.230849 11.7131 0.156031 11.5763C0.0857453 11.4478 0 11.2495 1.90735e-06 11.0107L0 9.7501C0 8.71126 0.588507 7.76158 1.52017 7.29699L1.5865 7.26391L1.75413 6.42799C1.55295 5.9214 1.5 5.34785 1.5 4.77735C1.5 3.90895 1.62745 3.01276 2.19275 2.29916C2.77172 1.56829 3.68694 1.17827 4.91718 1.04194C6.12298 0.90832 7.17884 1.07611 7.86079 1.80482ZM3.0231 7.7282L3 7.8434V12.0931C3.02086 12.1053 3.04268 12.1179 3.06543 12.131C3.32878 12.2821 3.71567 12.4861 4.19916 12.6907C5.17058 13.1017 6.50604 13.504 8 13.504C9.49396 13.504 10.8294 13.1017 11.8008 12.6907C12.2843 12.4861 12.6712 12.2821 12.9346 12.131C12.9573 12.1179 12.9791 12.1053 13 12.0931V7.8434L12.9769 7.7282C12.4867 7.93728 11.9022 8.01867 11.25 8.01867C10.1037 8.01867 9.19051 7.69201 8.54033 7.03004C8.3213 6.80703 8.14352 6.55741 8 6.28924C7.85648 6.55741 7.6787 6.80703 7.45967 7.03004C6.80949 7.69201 5.89633 8.01867 4.75 8.01867C4.09776 8.01867 3.51325 7.93728 3.0231 7.7282ZM6.76421 2.82557C6.57116 2.61928 6.12702 2.41307 5.08282 2.52878C4.06306 2.64179 3.60328 2.93176 3.36975 3.22656C3.12255 3.53861 3 4.01374 3 4.77735C3 5.56754 3.12905 5.94499 3.3082 6.1441C3.47045 6.32443 3.82768 6.52267 4.75 6.52267C5.60367 6.52267 6.08903 6.28769 6.38811 5.98319C6.70349 5.66209 6.91507 5.1591 7.00579 4.43524C7.12274 3.50212 6.96805 3.04338 6.76421 2.82557ZM9.23579 2.82557C9.03195 3.04338 8.87726 3.50212 8.99421 4.43524C9.08493 5.1591 9.29651 5.66209 9.61189 5.98319C9.91097 6.28769 10.3963 6.52267 11.25 6.52267C12.1723 6.52267 12.5295 6.32443 12.6918 6.1441C12.871 5.94499 13 5.56754 13 4.77735C13 4.01374 12.8775 3.53861 12.6303 3.22656C12.3967 2.93176 11.9369 2.64179 10.9172 2.52878C9.87298 2.41307 9.42884 2.61928 9.23579 2.82557Z"></path></svg>'},37165:M=>{M.exports='<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16"><path fill-rule="evenodd" d="M5.75 1a.75.75 0 00-.75.75v3c0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75v-3a.75.75 0 00-.75-.75h-4.5zm.75 3V2.5h3V4h-3zm-2.874-.467a.75.75 0 00-.752-1.298A1.75 1.75 0 002 3.75v9.5c0 .966.784 1.75 1.75 1.75h8.5A1.75 1.75 0 0014 13.25v-9.5a1.75 1.75 0 00-.874-1.515.75.75 0 10-.752 1.298.25.25 0 01.126.217v9.5a.25.25 0 01-.25.25h-8.5a.25.25 0 01-.25-.25v-9.5a.25.25 0 01.126-.217z"></path></svg>'},38440:M=>{M.exports='<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 28 28" version="1.1"><g id="surface1"><path style=" stroke:none;fill-rule:evenodd;fill:#FFFFFF;fill-opacity:1;" d="M 14 0 C 6.265625 0 0 6.265625 0 14 C 0 20.195312 4.007812 25.425781 9.574219 27.285156 C 10.273438 27.402344 10.535156 26.984375 10.535156 26.617188 C 10.535156 26.285156 10.515625 25.183594 10.515625 24.011719 C 7 24.660156 6.089844 23.152344 5.808594 22.363281 C 5.652344 21.960938 4.972656 20.722656 4.375 20.386719 C 3.886719 20.125 3.183594 19.476562 4.359375 19.460938 C 5.460938 19.441406 6.246094 20.476562 6.511719 20.894531 C 7.769531 23.011719 9.785156 22.417969 10.585938 22.050781 C 10.710938 21.140625 11.078125 20.527344 11.480469 20.175781 C 8.363281 19.828125 5.109375 18.621094 5.109375 13.265625 C 5.109375 11.742188 5.652344 10.484375 6.546875 9.503906 C 6.402344 9.152344 5.914062 7.714844 6.683594 5.792969 C 6.683594 5.792969 7.859375 5.425781 10.535156 7.226562 C 11.652344 6.914062 12.847656 6.753906 14.035156 6.753906 C 15.226562 6.753906 16.414062 6.914062 17.535156 7.226562 C 20.210938 5.410156 21.386719 5.792969 21.386719 5.792969 C 22.152344 7.714844 21.664062 9.152344 21.523438 9.503906 C 22.417969 10.484375 22.960938 11.726562 22.960938 13.265625 C 22.960938 18.636719 19.6875 19.828125 16.574219 20.175781 C 17.078125 20.613281 17.515625 21.453125 17.515625 22.765625 C 17.515625 24.640625 17.5 26.144531 17.5 26.617188 C 17.5 26.984375 17.761719 27.421875 18.460938 27.285156 C 24.160156 25.359375 27.996094 20.015625 28 14 C 28 6.265625 21.734375 0 14 0 Z M 14 0 "></path></g></svg>'},46279:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M10 3h3v1h-1v9l-1 1H4l-1-1V4H2V3h3V2a1 1 0 0 1 1-1h3a1 1 0 0 1 1 1v1zM9 2H6v1h3V2zM4 13h7V4H4v9zm2-8H5v7h1V5zm1 0h1v7H7V5zm2 0h1v7H9V5z" fill="#cccccc"></path></svg>'},19443:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M8 4C8.35556 4 8.71111 4.05333 9.06667 4.16C9.74222 4.33778 10.3289 4.67556 10.8267 5.17333C11.3244 5.67111 11.6622 6.25778 11.84 6.93333C11.9467 7.28889 12 7.64444 12 8C12 8.35556 11.9467 8.71111 11.84 9.06667C11.6622 9.74222 11.3244 10.3289 10.8267 10.8267C10.3289 11.3244 9.74222 11.6622 9.06667 11.84C8.71111 11.9467 8.35556 12 8 12C7.64444 12 7.28889 11.9467 6.93333 11.84C6.25778 11.6622 5.67111 11.3244 5.17333 10.8267C4.67556 10.3289 4.33778 9.74222 4.16 9.06667C4.05333 8.71111 4 8.35556 4 8C4 7.64444 4.03556 7.30667 4.10667 6.98667C4.21333 6.63111 4.35556 6.29333 4.53333 5.97333C4.88889 5.36889 5.36889 4.88889 5.97333 4.53333C6.29333 4.35556 6.61333 4.23111 6.93333 4.16C7.28889 4.05333 7.64444 4 8 4Z" fill="#CCCCCC"></path></svg>'},83962:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M2.40706 15L1 13.5929L3.35721 9.46781L3.52339 9.25025L11.7736 1L13.2321 1L15 2.76791V4.22636L6.74975 12.4766L6.53219 12.6428L2.40706 15ZM2.40706 13.5929L6.02053 11.7474L14.2708 3.49714L12.5029 1.72923L4.25262 9.97947L2.40706 13.5929Z" fill="#C5C5C5"></path><path fill-rule="evenodd" clip-rule="evenodd" d="M5.64642 12.3536L3.64642 10.3536L4.35353 9.64645L6.35353 11.6464L5.64642 12.3536Z" fill="#C5C5C5"></path></svg>'},93492:M=>{M.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M8.6 1c1.6.1 3.1.9 4.2 2 1.3 1.4 2 3.1 2 5.1 0 1.6-.6 3.1-1.6 4.4-1 1.2-2.4 2.1-4 2.4-1.6.3-3.2.1-4.6-.7-1.4-.8-2.5-2-3.1-3.5C.9 9.2.8 7.5 1.3 6c.5-1.6 1.4-2.9 2.8-3.8C5.4 1.3 7 .9 8.6 1zm.5 12.9c1.3-.3 2.5-1 3.4-2.1.8-1.1 1.3-2.4 1.2-3.8 0-1.6-.6-3.2-1.7-4.3-1-1-2.2-1.6-3.6-1.7-1.3-.1-2.7.2-3.8 1-1.1.8-1.9 1.9-2.3 3.3-.4 1.3-.4 2.7.2 4 .6 1.3 1.5 2.3 2.7 3 1.2.7 2.6.9 3.9.6zM7.9 7.5L10.3 5l.7.7-2.4 2.5 2.4 2.5-.7.7-2.4-2.5-2.4 2.5-.7-.7 2.4-2.5-2.4-2.5.7-.7 2.4 2.5z"></path></svg>'},92359:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M9.1 4.4L8.6 2H7.4L6.9 4.4L6.2 4.7L4.2 3.4L3.3 4.2L4.6 6.2L4.4 6.9L2 7.4V8.6L4.4 9.1L4.7 9.9L3.4 11.9L4.2 12.7L6.2 11.4L7 11.7L7.4 14H8.6L9.1 11.6L9.9 11.3L11.9 12.6L12.7 11.8L11.4 9.8L11.7 9L14 8.6V7.4L11.6 6.9L11.3 6.1L12.6 4.1L11.8 3.3L9.8 4.6L9.1 4.4ZM9.4 1L9.9 3.4L12 2.1L14 4.1L12.6 6.2L15 6.6V9.4L12.6 9.9L14 12L12 14L9.9 12.6L9.4 15H6.6L6.1 12.6L4 13.9L2 11.9L3.4 9.8L1 9.4V6.6L3.4 6.1L2.1 4L4.1 2L6.2 3.4L6.6 1H9.4ZM10 8C10 9.1 9.1 10 8 10C6.9 10 6 9.1 6 8C6 6.9 6.9 6 8 6C9.1 6 10 6.9 10 8ZM8 9C8.6 9 9 8.6 9 8C9 7.4 8.6 7 8 7C7.4 7 7 7.4 7 8C7 8.6 7.4 9 8 9Z" fill="#C5C5C5"></path></svg>'},80459:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M6.00012 13H7.00012L7.00012 7.00001L13.0001 7.00001V6.00001L7.00012 6.00001L7.00012 3H6.00012L6.00012 6.00001L3.00012 6.00001V7.00001H6.00012L6.00012 13Z" fill="#C5C5C5"></path><path fill-rule="evenodd" clip-rule="evenodd" d="M2.50012 2H13.5001L14.0001 2.5V13.5L13.5001 14H2.50012L2.00012 13.5V2.5L2.50012 2ZM3.00012 13H13.0001V3H3.00012V13Z" fill="#C5C5C5"></path></svg>'},40027:M=>{M.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path d="M7.50002 1C6.21445 1 4.95774 1.38123 3.88882 2.09546C2.8199 2.80969 1.98674 3.82485 1.49478 5.01257C1.00281 6.20029 0.874098 7.50719 1.1249 8.76807C1.37571 10.0289 1.99479 11.1872 2.90383 12.0962C3.81287 13.0052 4.97108 13.6243 6.23196 13.8751C7.49283 14.1259 8.79973 13.9972 9.98745 13.5052C11.1752 13.0133 12.1903 12.1801 12.9046 11.1112C13.6188 10.0423 14 8.78558 14 7.5C14 5.77609 13.3152 4.1228 12.0962 2.90381C10.8772 1.68482 9.22393 1 7.50002 1ZM7.50002 13C6.41223 13 5.34883 12.6775 4.44436 12.0731C3.53989 11.4688 2.83501 10.6097 2.41873 9.60474C2.00244 8.59974 1.89352 7.4939 2.10574 6.427C2.31796 5.36011 2.8418 4.38015 3.61099 3.61096C4.38018 2.84177 5.36013 2.31793 6.42703 2.10571C7.49392 1.89349 8.59977 2.00242 9.60476 2.4187C10.6098 2.83498 11.4688 3.53987 12.0731 4.44434C12.6775 5.34881 13 6.4122 13 7.5C13 8.95869 12.4205 10.3576 11.3891 11.389C10.3576 12.4205 8.95871 13 7.50002 13Z"></path><circle cx="7.50002" cy="7.5" r="1"></circle></svg>'},64674:M=>{M.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path d="M6.27 10.87h.71l4.56-4.56-.71-.71-4.2 4.21-1.92-1.92L4 8.6l2.27 2.27z"></path><path fill-rule="evenodd" clip-rule="evenodd" d="M8.6 1c1.6.1 3.1.9 4.2 2 1.3 1.4 2 3.1 2 5.1 0 1.6-.6 3.1-1.6 4.4-1 1.2-2.4 2.1-4 2.4-1.6.3-3.2.1-4.6-.7-1.4-.8-2.5-2-3.1-3.5C.9 9.2.8 7.5 1.3 6c.5-1.6 1.4-2.9 2.8-3.8C5.4 1.3 7 .9 8.6 1zm.5 12.9c1.3-.3 2.5-1 3.4-2.1.8-1.1 1.3-2.4 1.2-3.8 0-1.6-.6-3.2-1.7-4.3-1-1-2.2-1.6-3.6-1.7-1.3-.1-2.7.2-3.8 1-1.1.8-1.9 1.9-2.3 3.3-.4 1.3-.4 2.7.2 4 .6 1.3 1.5 2.3 2.7 3 1.2.7 2.6.9 3.9.6z"></path></svg>'},5064:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M13.2002 2H8.01724L7.66424 2.146L1.00024 8.81V9.517L6.18324 14.7H6.89024L9.10531 12.4853C9.65832 12.7768 10.2677 12.9502 10.8945 12.9923C11.659 13.0437 12.424 12.8981 13.1162 12.5694C13.8085 12.2407 14.4048 11.74 14.8483 11.1151C15.2918 10.4902 15.5676 9.76192 15.6492 9H15.6493C15.6759 8.83446 15.6929 8.66751 15.7003 8.5C15.6989 7.30693 15.2244 6.16311 14.3808 5.31948C14.1712 5.10988 13.9431 4.92307 13.7002 4.76064V2.5L13.2002 2ZM12.7002 4.25881C12.223 4.08965 11.7162 4.00057 11.2003 4C11.0676 4 10.9405 4.05268 10.8467 4.14645C10.7529 4.24021 10.7003 4.36739 10.7003 4.5C10.7003 4.63261 10.7529 4.75979 10.8467 4.85355C10.9405 4.94732 11.0676 5 11.2003 5C11.7241 5 12.2358 5.11743 12.7002 5.33771V7.476L8.77506 11.4005C8.75767 11.4095 8.74079 11.4194 8.72449 11.4304C8.6685 11.468 8.6207 11.5166 8.58397 11.5731C8.57475 11.5874 8.56627 11.602 8.55856 11.617L6.53624 13.639L2.06124 9.163L8.22424 3H12.7002V4.25881ZM13.7002 6.0505C14.3409 6.70435 14.7003 7.58365 14.7003 8.5C14.6955 8.66769 14.6784 8.8348 14.6493 9H14.6492C14.5675 9.58097 14.3406 10.1319 13.9894 10.6019C13.6383 11.0719 13.1743 11.4457 12.6403 11.6888C12.1063 11.9319 11.5197 12.0363 10.9346 11.9925C10.5622 11.9646 10.1982 11.8772 9.85588 11.7348L13.5542 8.037L13.7002 7.683V6.0505Z" fill="#C5C5C5"></path></svg>'},90346:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M4.99008 1C4.5965 1 4.21175 1.11671 3.8845 1.33538C3.55724 1.55404 3.30218 1.86484 3.15156 2.22846C3.00094 2.59208 2.96153 2.99221 3.03832 3.37823C3.1151 3.76425 3.30463 4.11884 3.58294 4.39714C3.83589 4.65009 4.15185 4.8297 4.49715 4.91798L4.49099 10.8286C4.40192 10.8517 4.31421 10.881 4.22852 10.9165C3.8649 11.0671 3.5541 11.3222 3.33544 11.6494C3.11677 11.9767 3.00006 12.3614 3.00006 12.755C3.00006 13.2828 3.20972 13.7889 3.58292 14.1621C3.95612 14.5353 4.46228 14.745 4.99006 14.745C5.38365 14.745 5.76839 14.6283 6.09565 14.4096C6.4229 14.191 6.67796 13.8802 6.82858 13.5165C6.9792 13.1529 7.01861 12.7528 6.94182 12.3668C6.86504 11.9807 6.67551 11.6262 6.3972 11.3479C6.14426 11.0949 5.8283 10.9153 5.48299 10.827V9.745H5.48915V8.80133C6.50043 10.3332 8.19531 11.374 10.1393 11.4893C10.2388 11.7413 10.3893 11.9714 10.5825 12.1648C10.8608 12.4432 11.2154 12.6328 11.6014 12.7097C11.9875 12.7866 12.3877 12.7472 12.7513 12.5966C13.115 12.446 13.4259 12.191 13.6446 11.8637C13.8633 11.5364 13.98 11.1516 13.98 10.758C13.98 10.2304 13.7705 9.72439 13.3975 9.35122C13.0245 8.97805 12.5186 8.76827 11.991 8.76801C11.5974 8.76781 11.2126 8.88435 10.8852 9.10289C10.5578 9.32144 10.3026 9.63216 10.1518 9.99577C10.0875 10.1509 10.0434 10.3127 10.0199 10.4772C7.48375 10.2356 5.48915 8.09947 5.48915 5.5C5.48915 5.33125 5.47282 5.16445 5.48915 5V4.9164C5.57823 4.89333 5.66594 4.86401 5.75162 4.82852C6.11525 4.6779 6.42604 4.42284 6.64471 4.09558C6.86337 3.76833 6.98008 3.38358 6.98008 2.99C6.98008 2.46222 6.77042 1.95605 6.39722 1.58286C6.02403 1.20966 5.51786 1 4.99008 1ZM4.99008 2C5.18593 1.9998 5.37743 2.0577 5.54037 2.16636C5.70331 2.27502 5.83035 2.42957 5.90544 2.61045C5.98052 2.79133 6.00027 2.99042 5.96218 3.18253C5.9241 3.37463 5.82989 3.55113 5.69147 3.68968C5.55306 3.82824 5.37666 3.92262 5.18459 3.9609C4.99252 3.99918 4.79341 3.97964 4.61246 3.90474C4.4315 3.82983 4.27682 3.70294 4.168 3.54012C4.05917 3.37729 4.00108 3.18585 4.00108 2.99C4.00135 2.72769 4.1056 2.47618 4.29098 2.29061C4.47637 2.10503 4.72777 2.00053 4.99008 2ZM4.99006 13.745C4.79422 13.7452 4.60271 13.6873 4.43977 13.5786C4.27684 13.47 4.14979 13.3154 4.07471 13.1345C3.99962 12.9537 3.97988 12.7546 4.01796 12.5625C4.05605 12.3704 4.15026 12.1939 4.28867 12.0553C4.42709 11.9168 4.60349 11.8224 4.79555 11.7841C4.98762 11.7458 5.18673 11.7654 5.36769 11.8403C5.54864 11.9152 5.70332 12.0421 5.81215 12.2049C5.92097 12.3677 5.97906 12.5591 5.97906 12.755C5.9788 13.0173 5.87455 13.2688 5.68916 13.4544C5.50377 13.64 5.25237 13.7445 4.99006 13.745ZM11.991 9.76801C12.1868 9.76801 12.3782 9.82607 12.541 9.93485C12.7038 10.0436 12.8307 10.1983 12.9057 10.3791C12.9806 10.56 13.0002 10.7591 12.962 10.9511C12.9238 11.1432 12.8295 11.3196 12.6911 11.458C12.5526 11.5965 12.3762 11.6908 12.1842 11.729C11.9921 11.7672 11.7931 11.7476 11.6122 11.6726C11.4313 11.5977 11.2767 11.4708 11.1679 11.308C11.0591 11.1452 11.001 10.9538 11.001 10.758C11.0013 10.4955 11.1057 10.2439 11.2913 10.0583C11.4769 9.87266 11.7285 9.76827 11.991 9.76801Z" fill="#C5C5C5"></path></svg>'},44370:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M10.5002 4.64639L8.35388 2.5H7.64677L5.50034 4.64639L6.20744 5.35349L7.3003 4.26066V5.27972H7.28082V5.73617L7.30568 5.73717C7.30768 5.84794 7.30968 5.95412 7.31169 6.05572C7.31538 6.24322 7.33201 6.43462 7.36158 6.62994C7.39114 6.82525 7.42994 7.02056 7.47799 7.21587C7.52603 7.41119 7.59255 7.62017 7.67755 7.84283C7.83276 8.22173 8.02124 8.56548 8.24297 8.87408C8.4647 9.18267 8.70307 9.47173 8.95806 9.74127C9.21306 10.0108 9.46621 10.2764 9.71751 10.5381C9.9688 10.7999 10.1961 11.0792 10.3993 11.376C10.6026 11.6729 10.767 11.9971 10.8927 12.3487C11.0183 12.7002 11.0812 13.1045 11.0812 13.5616V14.4463H12.5003V13.5616C12.4929 13.042 12.4375 12.5792 12.334 12.1729C12.2305 11.7667 12.0882 11.3995 11.9071 11.0713C11.7261 10.7432 11.5246 10.4444 11.3029 10.1749C11.0812 9.90533 10.8502 9.64752 10.61 9.40142C10.3698 9.15533 10.1388 8.90923 9.91707 8.66314C9.69533 8.41705 9.49392 8.15533 9.31284 7.87798C9.13176 7.60064 8.98763 7.29595 8.88046 6.96392C8.77329 6.63189 8.7197 6.25494 8.7197 5.83306V5.27972H8.71901V4.27935L9.79314 5.3535L10.5002 4.64639ZM7.04245 9.74127C7.15517 9.62213 7.26463 9.49917 7.37085 9.3724C7.12665 9.01878 6.92109 8.63423 6.75218 8.22189L6.74317 8.19952C6.70951 8.11134 6.67794 8.02386 6.6486 7.93713C6.47774 8.19261 6.28936 8.43461 6.08345 8.66314C5.86172 8.90923 5.63074 9.15533 5.39053 9.40142C5.15032 9.64752 4.91935 9.90533 4.69761 10.1749C4.47588 10.4444 4.27447 10.7432 4.09338 11.0713C3.9123 11.3995 3.77002 11.7667 3.66654 12.1729C3.56307 12.5792 3.50764 13.042 3.50024 13.5616V14.4463H4.91935V13.5616C4.91935 13.1045 4.98217 12.7002 5.10782 12.3487C5.23347 11.9971 5.39792 11.6729 5.60118 11.376C5.80444 11.0792 6.03171 10.7999 6.28301 10.5381C6.53431 10.2764 6.78746 10.0108 7.04245 9.74127Z" fill="#424242"></path></svg>'},20628:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M7.99976 1H6.99976V3H1.49976L0.999756 3.5V7.5L1.49976 8H6.99976V15H7.99976V8H12.4898L12.8298 7.87L15.0098 5.87V5.13L12.8298 3.13L12.4998 3H7.99976V1ZM12.2898 7H1.99976V4H12.2898L13.9198 5.5L12.2898 7ZM4.99976 5H9.99976V6H4.99976V5Z" fill="#C5C5C5"></path></svg>'},15010:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M14 7V8H8V14H7V8H1V7H7V1H8V7H14Z" fill="#C5C5C5"></path></svg>'},14268:M=>{M.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M5.616 4.928a2.487 2.487 0 0 1-1.119.922c-.148.06-.458.138-.458.138v5.008a2.51 2.51 0 0 1 1.579 1.062c.273.412.419.895.419 1.388.008.343-.057.684-.19 1A2.485 2.485 0 0 1 3.5 15.984a2.482 2.482 0 0 1-1.388-.419A2.487 2.487 0 0 1 1.05 13c.095-.486.331-.932.68-1.283.349-.343.79-.579 1.269-.68V5.949a2.6 2.6 0 0 1-1.269-.68 2.503 2.503 0 0 1-.68-1.283 2.487 2.487 0 0 1 1.06-2.565A2.49 2.49 0 0 1 3.5 1a2.504 2.504 0 0 1 1.807.729 2.493 2.493 0 0 1 .729 1.81c.002.494-.144.978-.42 1.389zm-.756 7.861a1.5 1.5 0 0 0-.552-.579 1.45 1.45 0 0 0-.77-.21 1.495 1.495 0 0 0-1.47 1.79 1.493 1.493 0 0 0 1.18 1.179c.288.058.586.03.86-.08.276-.117.512-.312.68-.56.15-.226.235-.49.249-.76a1.51 1.51 0 0 0-.177-.78zM2.708 4.741c.247.161.536.25.83.25.271 0 .538-.075.77-.211a1.514 1.514 0 0 0 .729-1.359 1.513 1.513 0 0 0-.25-.76 1.551 1.551 0 0 0-.68-.56 1.49 1.49 0 0 0-.86-.08 1.494 1.494 0 0 0-1.179 1.18c-.058.288-.03.586.08.86.117.276.312.512.56.68zm10.329 6.296c.48.097.922.335 1.269.68.466.47.729 1.107.725 1.766.002.493-.144.977-.42 1.388a2.499 2.499 0 0 1-4.532-.899 2.5 2.5 0 0 1 1.067-2.565c.267-.183.571-.308.889-.37V5.489a1.5 1.5 0 0 0-1.5-1.499H8.687l1.269 1.27-.71.709L7.117 3.84v-.7l2.13-2.13.71.711-1.269 1.27h1.85a2.484 2.484 0 0 1 2.312 1.541c.125.302.189.628.187.957v5.548zm.557 3.509a1.493 1.493 0 0 0 .191-1.89 1.552 1.552 0 0 0-.68-.559 1.49 1.49 0 0 0-.86-.08 1.493 1.493 0 0 0-1.179 1.18 1.49 1.49 0 0 0 .08.86 1.496 1.496 0 0 0 2.448.49z"></path></svg>'},30340:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M7.38893 12.9906L6.11891 11.7205L6.78893 11.0206L8.91893 13.1506V13.8505L6.78893 15.9805L6.07893 15.2706L7.34892 14.0006H5.49892C5.17024 14.0019 4.84458 13.9381 4.54067 13.8129C4.23675 13.6878 3.96061 13.5037 3.7282 13.2713C3.49579 13.0389 3.31171 12.7627 3.18654 12.4588C3.06137 12.1549 2.99759 11.8292 2.99892 11.5006V5.95052C2.5198 5.84851 2.07944 5.61279 1.72893 5.27059C1.3808 4.91884 1.14393 4.47238 1.0479 3.98689C0.951867 3.50141 1.00092 2.9984 1.18892 2.54061C1.37867 2.08436 1.69938 1.69458 2.11052 1.42049C2.52166 1.14639 3.00479 1.00024 3.49892 1.00057C3.84188 0.993194 4.18256 1.05787 4.49892 1.19051C4.80197 1.31518 5.07732 1.49871 5.30904 1.73042C5.54075 1.96214 5.72425 2.23755 5.84892 2.54061C5.98157 2.85696 6.0463 3.19765 6.03893 3.54061C6.03926 4.03474 5.89316 4.51789 5.61906 4.92903C5.34497 5.34017 4.95516 5.6608 4.49892 5.85054C4.35057 5.91224 4.19649 5.95915 4.03893 5.99056V11.4906C4.03893 11.8884 4.19695 12.2699 4.47826 12.5512C4.75956 12.8325 5.1411 12.9906 5.53893 12.9906H7.38893ZM2.70894 4.74056C2.95497 4.90376 3.24368 4.99072 3.53893 4.99056C3.81026 4.99066 4.07654 4.91718 4.3094 4.77791C4.54227 4.63864 4.73301 4.43877 4.86128 4.19966C4.98956 3.96056 5.05057 3.69116 5.03783 3.42012C5.02508 3.14908 4.93907 2.88661 4.78893 2.6606C4.62119 2.4121 4.38499 2.21751 4.10893 2.10054C3.83645 1.98955 3.53719 1.96176 3.24892 2.02059C2.95693 2.07705 2.68852 2.2196 2.47823 2.42989C2.26793 2.64018 2.12539 2.90853 2.06892 3.20052C2.0101 3.4888 2.03792 3.78802 2.14891 4.0605C2.26588 4.33656 2.46043 4.57282 2.70894 4.74056ZM13.0389 11.0406C13.5196 11.1384 13.9612 11.3747 14.309 11.7206C14.7766 12.191 15.039 12.8273 15.0389 13.4906C15.0393 13.9847 14.8932 14.4679 14.6191 14.879C14.345 15.2902 13.9552 15.6109 13.499 15.8007C13.0416 15.9915 12.5378 16.0421 12.0516 15.946C11.5654 15.85 11.1187 15.6117 10.7683 15.2612C10.4179 14.9108 10.1795 14.4641 10.0835 13.9779C9.98746 13.4917 10.0381 12.988 10.2289 12.5306C10.4218 12.0768 10.7412 11.688 11.1489 11.4106C11.4177 11.2286 11.7204 11.1028 12.0389 11.0406V5.4906C12.0389 5.09278 11.8809 4.71124 11.5996 4.42993C11.3183 4.14863 10.9368 3.9906 10.5389 3.9906H8.68896L9.95892 5.26062L9.24896 5.97058L7.11893 3.84058V3.14063L9.24896 1.01062L9.95892 1.72058L8.68896 2.9906H10.5389C10.8676 2.98928 11.1933 3.05305 11.4972 3.17822C11.8011 3.30339 12.0772 3.48744 12.3096 3.71985C12.542 3.95226 12.7262 4.22844 12.8513 4.53235C12.9765 4.83626 13.0403 5.16193 13.0389 5.4906V11.0406ZM12.6879 14.9829C13.0324 14.9483 13.3542 14.7956 13.5989 14.5507C13.8439 14.306 13.9966 13.984 14.0313 13.6395C14.0659 13.295 13.9803 12.9492 13.7889 12.6606C13.6212 12.4121 13.385 12.2176 13.1089 12.1006C12.8365 11.9896 12.5372 11.9618 12.249 12.0206C11.957 12.0771 11.6886 12.2196 11.4783 12.4299C11.268 12.6402 11.1254 12.9086 11.069 13.2006C11.0101 13.4888 11.0379 13.7881 11.1489 14.0605C11.2659 14.3366 11.4604 14.5729 11.7089 14.7406C11.9975 14.9319 12.3434 15.0175 12.6879 14.9829Z" fill="#C5C5C5"></path></svg>'},90659:M=>{M.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M5.61594 4.92769C5.34304 5.33899 4.95319 5.66062 4.49705 5.8497C4.34891 5.91013 4.03897 5.9881 4.03897 5.9881V10.9958C4.19686 11.027 4.35086 11.0738 4.499 11.1362C4.95513 11.3272 5.34304 11.6469 5.61789 12.0582C5.89079 12.4695 6.03699 12.9529 6.03699 13.4461C6.04478 13.7891 5.98046 14.1303 5.84791 14.446C5.72315 14.7482 5.53992 15.023 5.30796 15.255C5.07794 15.487 4.80114 15.6702 4.499 15.7949C4.18322 15.9275 3.84209 15.9918 3.49902 15.984C3.00585 15.986 2.52243 15.8398 2.11113 15.5649C1.69983 15.292 1.3782 14.9022 1.18912 14.446C1.00198 13.988 0.953253 13.485 1.04877 12.9997C1.14428 12.5143 1.38015 12.0679 1.72907 11.717C2.07799 11.374 2.51853 11.1381 2.99805 11.0367V5.94911C2.52048 5.8458 2.07994 5.61189 1.72907 5.26881C1.38015 4.91794 1.14428 4.47155 1.04877 3.98618C0.951304 3.50081 1.00004 2.99789 1.18912 2.53981C1.3782 2.08368 1.69983 1.69382 2.11113 1.42092C2.52048 1.14607 3.0039 0.999877 3.49902 0.999877C3.84014 0.99403 4.18127 1.05836 4.49705 1.18896C4.79919 1.31371 5.07404 1.49695 5.30601 1.72891C5.53797 1.96087 5.7212 2.23767 5.84596 2.53981C5.97851 2.8556 6.04284 3.19672 6.03504 3.5398C6.03699 4.03296 5.89079 4.51639 5.61594 4.92769ZM4.85962 12.7892C4.73097 12.5494 4.53994 12.3486 4.30797 12.2102C4.07601 12.0699 3.80896 11.9958 3.538 11.9997C3.24171 11.9997 2.95322 12.0855 2.70761 12.2492C2.46005 12.4168 2.26512 12.6527 2.14816 12.9295C2.03706 13.2024 2.00977 13.5006 2.06824 13.7891C2.12477 14.0796 2.26707 14.3486 2.47759 14.5591C2.68812 14.7696 2.95517 14.9119 3.24756 14.9685C3.53606 15.0269 3.8343 14.9996 4.1072 14.8885C4.38399 14.7716 4.61986 14.5766 4.7875 14.3291C4.93759 14.103 5.02336 13.8398 5.037 13.5689C5.0487 13.2979 4.98827 13.0289 4.85962 12.7892ZM2.70761 4.74056C2.95517 4.90235 3.24366 4.99006 3.538 4.99006C3.80896 4.99006 4.07601 4.91599 4.30797 4.77954C4.53994 4.63919 4.73097 4.44037 4.85962 4.2006C4.98827 3.96084 5.05065 3.69184 5.037 3.42089C5.02336 3.14994 4.93759 2.88679 4.7875 2.66067C4.61986 2.41311 4.38399 2.21818 4.1072 2.10122C3.8343 1.99011 3.53606 1.96282 3.24756 2.0213C2.95712 2.07783 2.68812 2.22013 2.47759 2.43065C2.26707 2.64118 2.12477 2.90823 2.06824 3.20062C2.00977 3.48911 2.03706 3.78735 2.14816 4.06025C2.26512 4.33705 2.46005 4.57292 2.70761 4.74056ZM13.0368 11.0368C13.5164 11.1342 13.9588 11.372 14.3058 11.7171C14.7717 12.1868 15.0348 12.8243 15.0309 13.4831C15.0329 13.9763 14.8867 14.4597 14.6119 14.871C14.339 15.2823 13.9491 15.6039 13.493 15.793C13.0368 15.984 12.532 16.0347 12.0466 15.9392C11.5612 15.8437 11.1148 15.6059 10.764 15.255C10.415 14.9041 10.1753 14.4578 10.0798 13.9724C9.98425 13.487 10.0349 12.9841 10.226 12.526C10.4189 12.0738 10.7386 11.6839 11.146 11.4071C11.4131 11.2239 11.7172 11.0991 12.0349 11.0368V7.4891H13.0368V11.0368ZM13.5943 14.5455C13.8399 14.3018 13.992 13.9802 14.0271 13.6352C14.0622 13.2921 13.9764 12.9451 13.7854 12.6566C13.6177 12.4091 13.3819 12.2141 13.1051 12.0972C12.8322 11.9861 12.5339 11.9588 12.2454 12.0173C11.955 12.0738 11.686 12.2161 11.4755 12.4266C11.2649 12.6371 11.1226 12.9042 11.0661 13.1966C11.0076 13.4851 11.0349 13.7833 11.146 14.0562C11.263 14.333 11.4579 14.5689 11.7055 14.7365C11.994 14.9275 12.339 15.0133 12.684 14.9782C13.0271 14.9431 13.3507 14.7911 13.5943 14.5455Z"></path><path fill-rule="evenodd" clip-rule="evenodd" d="M11.6876 3.40036L10 5.088L10.7071 5.7951L12.3947 4.10747L14.0824 5.7951L14.7895 5.088L13.1019 3.40036L14.7895 1.71272L14.0824 1.00562L12.3947 2.69325L10.7071 1.00562L10 1.71272L11.6876 3.40036Z"></path></svg>'},83344:M=>{M.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path d="M4.49705 5.8497C4.95319 5.66062 5.34304 5.33899 5.61594 4.92769C5.89079 4.51639 6.03699 4.03296 6.03504 3.5398C6.04284 3.19672 5.97851 2.8556 5.84596 2.53981C5.7212 2.23767 5.53797 1.96087 5.30601 1.72891C5.07404 1.49695 4.79919 1.31371 4.49705 1.18896C4.18127 1.05836 3.84014 0.99403 3.49902 0.999877C3.0039 0.999877 2.52048 1.14607 2.11113 1.42092C1.69983 1.69382 1.3782 2.08368 1.18912 2.53981C1.00004 2.99789 0.951304 3.50081 1.04877 3.98618C1.14428 4.47155 1.38015 4.91794 1.72907 5.26881C2.07994 5.61189 2.52048 5.8458 2.99805 5.94911V11.0367C2.51853 11.1381 2.07799 11.374 1.72907 11.717C1.38015 12.0679 1.14428 12.5143 1.04877 12.9997C0.953253 13.485 1.00198 13.988 1.18912 14.446C1.3782 14.9022 1.69983 15.292 2.11113 15.5649C2.52243 15.8398 3.00585 15.986 3.49902 15.984C3.84209 15.9918 4.18322 15.9275 4.499 15.7949C4.80114 15.6702 5.07794 15.487 5.30796 15.255C5.53992 15.023 5.72315 14.7482 5.84791 14.446C5.98046 14.1303 6.04478 13.7891 6.03699 13.4461C6.03699 12.9529 5.89079 12.4695 5.61789 12.0582C5.34304 11.6469 4.95513 11.3272 4.499 11.1362C4.35086 11.0738 4.19686 11.027 4.03897 10.9958V5.9881C4.03897 5.9881 4.34891 5.91013 4.49705 5.8497ZM4.30797 12.2102C4.53994 12.3486 4.73097 12.5494 4.85962 12.7892C4.98827 13.0289 5.0487 13.2979 5.037 13.5689C5.02336 13.8398 4.93759 14.103 4.7875 14.3291C4.61986 14.5766 4.38399 14.7716 4.1072 14.8885C3.8343 14.9996 3.53606 15.0269 3.24756 14.9685C2.95517 14.9119 2.68812 14.7696 2.47759 14.5591C2.26707 14.3486 2.12477 14.0796 2.06824 13.7891C2.00977 13.5006 2.03706 13.2024 2.14816 12.9295C2.26512 12.6527 2.46005 12.4168 2.70761 12.2492C2.95322 12.0855 3.24171 11.9997 3.538 11.9997C3.80896 11.9958 4.07601 12.0699 4.30797 12.2102ZM3.538 4.99006C3.24366 4.99006 2.95517 4.90235 2.70761 4.74056C2.46005 4.57292 2.26512 4.33705 2.14816 4.06025C2.03706 3.78735 2.00977 3.48911 2.06824 3.20062C2.12477 2.90823 2.26707 2.64118 2.47759 2.43065C2.68812 2.22013 2.95712 2.07783 3.24756 2.0213C3.53606 1.96282 3.8343 1.99011 4.1072 2.10122C4.38399 2.21818 4.61986 2.41311 4.7875 2.66067C4.93759 2.88679 5.02336 3.14994 5.037 3.42089C5.05065 3.69184 4.98827 3.96084 4.85962 4.2006C4.73097 4.44037 4.53994 4.63919 4.30797 4.77954C4.07601 4.91599 3.80896 4.99006 3.538 4.99006Z"></path><path fill-rule="evenodd" clip-rule="evenodd" d="M15.0543 13.5C15.0543 14.8807 13.935 16 12.5543 16C11.1736 16 10.0543 14.8807 10.0543 13.5C10.0543 12.1193 11.1736 11 12.5543 11C13.935 11 15.0543 12.1193 15.0543 13.5ZM12.5543 15C13.3827 15 14.0543 14.3284 14.0543 13.5C14.0543 12.6716 13.3827 12 12.5543 12C11.7258 12 11.0543 12.6716 11.0543 13.5C11.0543 14.3284 11.7258 15 12.5543 15Z"></path><circle cx="12.5543" cy="7.75073" r="1"></circle><circle cx="12.5543" cy="3.50146" r="1"></circle></svg>'},9649:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M2.14648 6.3065L6.16649 2.2865L6.87359 2.2865L10.8936 6.3065L10.1865 7.0136L6.97998 3.8071L6.97998 5.69005C6.97998 8.50321 7.58488 10.295 8.70856 11.3953C9.83407 12.4974 11.5857 13.0101 14.13 13.0101L14.48 13.0101L14.48 14.0101L14.13 14.0101C11.4843 14.0101 9.4109 13.4827 8.00891 12.1098C6.60509 10.7351 5.97998 8.61689 5.97998 5.69005L5.97998 3.88722L2.85359 7.01361L2.14648 6.3065Z" fill="#C5C5C5"></path></svg>'},72362:M=>{M.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M7.16 3.5C4.73 5.06 3.55 6.67 3.55 9.36c.16-.05.3-.05.44-.05 1.27 0 2.5.86 2.5 2.41 0 1.61-1.03 2.61-2.5 2.61-1.9 0-2.99-1.52-2.99-4.25 0-3.8 1.75-6.53 5.02-8.42L7.16 3.5zm7 0c-2.43 1.56-3.61 3.17-3.61 5.86.16-.05.3-.05.44-.05 1.27 0 2.5.86 2.5 2.41 0 1.61-1.03 2.61-2.5 2.61-1.89 0-2.98-1.52-2.98-4.25 0-3.8 1.75-6.53 5.02-8.42l1.14 1.84h-.01z"></path></svg>'},98923:M=>{M.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M10.7099 1.29L13.7099 4.29L13.9999 5V14L12.9999 15H3.99994L2.99994 14V2L3.99994 1H9.99994L10.7099 1.29ZM3.99994 14H12.9999V5L9.99994 2H3.99994V14ZM8 6H6V7H8V9H9V7H11V6H9V4H8V6ZM6 11H11V12H6V11Z"></path></svg>'},96855:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M7.54883 10.0781C8.00911 10.2604 8.42839 10.502 8.80664 10.8027C9.1849 11.1035 9.50846 11.4521 9.77734 11.8486C10.0462 12.2451 10.2536 12.6712 10.3994 13.127C10.5452 13.5827 10.6204 14.0612 10.625 14.5625V15H9.75V14.5625C9.75 14.0202 9.64746 13.5098 9.44238 13.0312C9.2373 12.5527 8.95475 12.1357 8.59473 11.7803C8.2347 11.4248 7.81771 11.1445 7.34375 10.9395C6.86979 10.7344 6.35938 10.6296 5.8125 10.625C5.27018 10.625 4.75977 10.7275 4.28125 10.9326C3.80273 11.1377 3.38574 11.4202 3.03027 11.7803C2.6748 12.1403 2.39453 12.5573 2.18945 13.0312C1.98438 13.5052 1.87956 14.0156 1.875 14.5625V15H1V14.5625C1 14.0658 1.07292 13.5872 1.21875 13.127C1.36458 12.6667 1.57422 12.2406 1.84766 11.8486C2.12109 11.4567 2.44466 11.1104 2.81836 10.8096C3.19206 10.5088 3.61133 10.265 4.07617 10.0781C3.87109 9.93685 3.68652 9.77279 3.52246 9.58594C3.3584 9.39909 3.2194 9.19857 3.10547 8.98438C2.99154 8.77018 2.90495 8.54232 2.8457 8.30078C2.78646 8.05924 2.75456 7.81315 2.75 7.5625C2.75 7.13867 2.82975 6.74219 2.98926 6.37305C3.14876 6.00391 3.36751 5.68034 3.64551 5.40234C3.9235 5.12435 4.24707 4.9056 4.61621 4.74609C4.98535 4.58659 5.38411 4.50456 5.8125 4.5C6.23633 4.5 6.63281 4.57975 7.00195 4.73926C7.37109 4.89876 7.69466 5.11751 7.97266 5.39551C8.25065 5.6735 8.4694 5.99707 8.62891 6.36621C8.78841 6.73535 8.87044 7.13411 8.875 7.5625C8.875 7.81315 8.84538 8.05697 8.78613 8.29395C8.72689 8.53092 8.63802 8.75879 8.51953 8.97754C8.40104 9.19629 8.26204 9.39909 8.10254 9.58594C7.94303 9.77279 7.75846 9.93685 7.54883 10.0781ZM5.8125 9.75C6.11328 9.75 6.39583 9.69303 6.66016 9.5791C6.92448 9.46517 7.15462 9.31022 7.35059 9.11426C7.54655 8.91829 7.70378 8.68587 7.82227 8.41699C7.94076 8.14811 8 7.86328 8 7.5625C8 7.26172 7.94303 6.97917 7.8291 6.71484C7.71517 6.45052 7.55794 6.22038 7.35742 6.02441C7.1569 5.82845 6.92448 5.67122 6.66016 5.55273C6.39583 5.43424 6.11328 5.375 5.8125 5.375C5.51172 5.375 5.22917 5.43197 4.96484 5.5459C4.70052 5.65983 4.4681 5.81706 4.26758 6.01758C4.06706 6.2181 3.90983 6.45052 3.7959 6.71484C3.68197 6.97917 3.625 7.26172 3.625 7.5625C3.625 7.86328 3.68197 8.14583 3.7959 8.41016C3.90983 8.67448 4.06478 8.9069 4.26074 9.10742C4.45671 9.30794 4.68913 9.46517 4.95801 9.5791C5.22689 9.69303 5.51172 9.75 5.8125 9.75ZM15 1V8H13.25L10.625 10.625V8H9.75V7.125H11.5V8.5127L12.8877 7.125H14.125V1.875H5.375V3.44727C5.22917 3.46549 5.08333 3.48828 4.9375 3.51562C4.79167 3.54297 4.64583 3.58398 4.5 3.63867V1H15Z" fill="#C5C5C5"></path></svg>'},15493:M=>{M.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M9.12 4.37333L8.58667 1.97333H7.41333L6.88 4.37333L6.18667 4.69333L4.21333 3.41333L3.30667 4.21333L4.58667 6.18667L4.42667 6.88L2.02667 7.41333V8.58667L4.42667 9.12L4.69333 9.92L3.41333 11.8933L4.21333 12.6933L6.18667 11.4133L6.98667 11.68L7.41333 13.9733H8.58667L9.12 11.5733L9.92 11.3067L11.8933 12.5867L12.6933 11.7867L11.4133 9.81333L11.68 9.01333L14.0267 8.58667V7.41333L11.6267 6.88L11.3067 6.08L12.5867 4.10667L11.7867 3.30667L9.81333 4.58667L9.12 4.37333ZM9.38667 1.01333L9.92 3.41333L12 2.08L14.0267 4.10667L12.5867 6.18667L14.9867 6.61333V9.38667L12.5867 9.92L14.0267 12L12 13.9733L9.92 12.5867L9.38667 14.9867H6.61333L6.08 12.5867L4 13.92L2.02667 11.8933L3.41333 9.81333L1.01333 9.38667V6.61333L3.41333 6.08L2.08 4L4.10667 1.97333L6.18667 3.41333L6.61333 1.01333H9.38667ZM10.0267 8C10.0267 8.53333 9.81333 8.99556 9.38667 9.38667C8.99556 9.77778 8.53333 9.97333 8 9.97333C7.46667 9.97333 7.00444 9.77778 6.61333 9.38667C6.22222 8.99556 6.02667 8.53333 6.02667 8C6.02667 7.46667 6.22222 7.00444 6.61333 6.61333C7.00444 6.18667 7.46667 5.97333 8 5.97333C8.53333 5.97333 8.99556 6.18667 9.38667 6.61333C9.81333 7.00444 10.0267 7.46667 10.0267 8ZM8 9.01333C8.28444 9.01333 8.51556 8.92444 8.69333 8.74667C8.90667 8.53333 9.01333 8.28444 9.01333 8C9.01333 7.71556 8.90667 7.48444 8.69333 7.30667C8.51556 7.09333 8.28444 6.98667 8 6.98667C7.71556 6.98667 7.46667 7.09333 7.25333 7.30667C7.07556 7.48444 6.98667 7.71556 6.98667 8C6.98667 8.28444 7.07556 8.53333 7.25333 8.74667C7.46667 8.92444 7.71556 9.01333 8 9.01333Z" fill="#CCCCCC"></path></svg>'},61779:M=>{M.exports='<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M17.28 7.78a.75.75 0 00-1.06-1.06l-9.5 9.5a.75.75 0 101.06 1.06l9.5-9.5z"></path><path fill-rule="evenodd" d="M12 1C5.925 1 1 5.925 1 12s4.925 11 11 11 11-4.925 11-11S18.075 1 12 1zM2.5 12a9.5 9.5 0 1119 0 9.5 9.5 0 01-19 0z"></path></svg>'},70596:M=>{M.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path d="M5.39804 10.8069C5.57428 10.9312 5.78476 10.9977 6.00043 10.9973C6.21633 10.9975 6.42686 10.93 6.60243 10.8043C6.77993 10.6739 6.91464 10.4936 6.98943 10.2863L7.43643 8.91335C7.55086 8.56906 7.74391 8.25615 8.00028 7.99943C8.25665 7.74272 8.56929 7.54924 8.91343 7.43435L10.3044 6.98335C10.4564 6.92899 10.5936 6.84019 10.7055 6.7239C10.8174 6.60762 10.9008 6.467 10.9492 6.31308C10.9977 6.15916 11.0098 5.99611 10.9847 5.83672C10.9596 5.67732 10.8979 5.52591 10.8044 5.39435C10.6703 5.20842 10.4794 5.07118 10.2604 5.00335L8.88543 4.55635C8.54091 4.44212 8.22777 4.24915 7.97087 3.99277C7.71396 3.73638 7.52035 3.42363 7.40543 3.07935L6.95343 1.69135C6.88113 1.48904 6.74761 1.31428 6.57143 1.19135C6.43877 1.09762 6.28607 1.03614 6.12548 1.01179C5.96489 0.987448 5.80083 1.00091 5.64636 1.05111C5.49188 1.1013 5.35125 1.18685 5.23564 1.30095C5.12004 1.41505 5.03265 1.55454 4.98043 1.70835L4.52343 3.10835C4.40884 3.44317 4.21967 3.74758 3.97022 3.9986C3.72076 4.24962 3.41753 4.44067 3.08343 4.55735L1.69243 5.00535C1.54065 5.05974 1.40352 5.14852 1.29177 5.26474C1.18001 5.38095 1.09666 5.52145 1.04824 5.67523C0.999819 5.82902 0.987639 5.99192 1.01265 6.1512C1.03767 6.31048 1.0992 6.46181 1.19243 6.59335C1.32027 6.7728 1.50105 6.90777 1.70943 6.97935L3.08343 7.42435C3.52354 7.57083 3.90999 7.84518 4.19343 8.21235C4.35585 8.42298 4.4813 8.65968 4.56443 8.91235L5.01643 10.3033C5.08846 10.5066 5.22179 10.6826 5.39804 10.8069ZM5.48343 3.39235L6.01043 2.01535L6.44943 3.39235C6.61312 3.8855 6.88991 4.33351 7.25767 4.70058C7.62544 5.06765 8.07397 5.34359 8.56743 5.50635L9.97343 6.03535L8.59143 6.48335C8.09866 6.64764 7.65095 6.92451 7.28382 7.29198C6.9167 7.65945 6.64026 8.10742 6.47643 8.60035L5.95343 9.97835L5.50443 8.59935C5.34335 8.10608 5.06943 7.65718 4.70443 7.28835C4.3356 6.92031 3.88653 6.64272 3.39243 6.47735L2.01443 5.95535L3.40043 5.50535C3.88672 5.33672 4.32775 5.05855 4.68943 4.69235C5.04901 4.32464 5.32049 3.88016 5.48343 3.39235ZM11.5353 14.8494C11.6713 14.9456 11.8337 14.9973 12.0003 14.9974C12.1654 14.9974 12.3264 14.9464 12.4613 14.8514C12.6008 14.7529 12.7058 14.6129 12.7613 14.4514L13.0093 13.6894C13.0625 13.5309 13.1515 13.3869 13.2693 13.2684C13.3867 13.1498 13.5307 13.0611 13.6893 13.0094L14.4613 12.7574C14.619 12.7029 14.7557 12.6004 14.8523 12.4644C14.9257 12.3614 14.9736 12.2424 14.9921 12.1173C15.0106 11.9922 14.9992 11.8645 14.9588 11.7447C14.9184 11.6249 14.8501 11.5163 14.7597 11.428C14.6692 11.3396 14.5591 11.2739 14.4383 11.2364L13.6743 10.9874C13.5162 10.9348 13.3724 10.8462 13.2544 10.7285C13.1364 10.6109 13.0473 10.4674 12.9943 10.3094L12.7423 9.53638C12.6886 9.37853 12.586 9.24191 12.4493 9.14638C12.3473 9.07343 12.2295 9.02549 12.1056 9.00642C11.9816 8.98736 11.8549 8.99772 11.7357 9.03665C11.6164 9.07558 11.508 9.142 11.4192 9.23054C11.3304 9.31909 11.2636 9.42727 11.2243 9.54638L10.9773 10.3084C10.925 10.466 10.8375 10.6097 10.7213 10.7284C10.6066 10.8449 10.4667 10.9335 10.3123 10.9874L9.53931 11.2394C9.38025 11.2933 9.2422 11.3959 9.1447 11.5326C9.04721 11.6694 8.99522 11.8333 8.99611 12.0013C8.99699 12.1692 9.0507 12.3326 9.14963 12.4683C9.24856 12.604 9.38769 12.7051 9.54731 12.7574L10.3103 13.0044C10.4692 13.0578 10.6136 13.1471 10.7323 13.2654C10.8505 13.3836 10.939 13.5283 10.9903 13.6874L11.2433 14.4614C11.2981 14.6178 11.4001 14.7534 11.5353 14.8494ZM10.6223 12.0564L10.4433 11.9974L10.6273 11.9334C10.9291 11.8284 11.2027 11.6556 11.4273 11.4284C11.6537 11.1994 11.8248 10.9216 11.9273 10.6164L11.9853 10.4384L12.0443 10.6194C12.1463 10.9261 12.3185 11.2047 12.5471 11.4332C12.7757 11.6617 13.0545 11.8336 13.3613 11.9354L13.5563 11.9984L13.3763 12.0574C13.0689 12.1596 12.7898 12.3322 12.5611 12.5616C12.3324 12.791 12.1606 13.0707 12.0593 13.3784L12.0003 13.5594L11.9423 13.3784C11.8409 13.0702 11.6687 12.7901 11.4394 12.5605C11.2102 12.3309 10.9303 12.1583 10.6223 12.0564Z"></path></svg>'},33027:M=>{M.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path d="M6 6h4v4H6z"></path><path fill-rule="evenodd" clip-rule="evenodd" d="M8.6 1c1.6.1 3.1.9 4.2 2 1.3 1.4 2 3.1 2 5.1 0 1.6-.6 3.1-1.6 4.4-1 1.2-2.4 2.1-4 2.4-1.6.3-3.2.1-4.6-.7-1.4-.8-2.5-2-3.1-3.5C.9 9.2.8 7.5 1.3 6c.5-1.6 1.4-2.9 2.8-3.8C5.4 1.3 7 .9 8.6 1zm.5 12.9c1.3-.3 2.5-1 3.4-2.1.8-1.1 1.3-2.4 1.2-3.8 0-1.6-.6-3.2-1.7-4.3-1-1-2.2-1.6-3.6-1.7-1.3-.1-2.7.2-3.8 1-1.1.8-1.9 1.9-2.3 3.3-.4 1.3-.4 2.7.2 4 .6 1.3 1.5 2.3 2.7 3 1.2.7 2.6.9 3.9.6z"></path></svg>'},17411:M=>{M.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M2.006 8.267L.78 9.5 0 8.73l2.09-2.07.76.01 2.09 2.12-.76.76-1.167-1.18a5 5 0 0 0 9.4 1.983l.813.597a6 6 0 0 1-11.22-2.683zm10.99-.466L11.76 6.55l-.76.76 2.09 2.11.76.01 2.09-2.07-.75-.76-1.194 1.18a6 6 0 0 0-11.11-2.92l.81.594a5 5 0 0 1 9.3 2.346z"></path></svg>'},65013:M=>{M.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M3.57 6.699l5.693-4.936L8.585 1 3.273 5.596l-1.51-1.832L1 4.442l1.85 2.214.72.043zM15 5H6.824l2.307-2H15v2zM6 7h9v2H6V7zm9 4H6v2h9v-2z"></path></svg>'},2481:M=>{M.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M14 5H2V3h12v2zm0 4H2V7h12v2zM2 13h12v-2H2v2z"></path></svg>'}},li={};function fe(M){var R=li[M];if(R!==void 0)return R.exports;var J=li[M]={id:M,exports:{}};return Il[M].call(J.exports,J,J.exports,fe),J.exports}i(fe,"__webpack_require__"),fe.n=M=>{var R=M&&M.__esModule?()=>M.default:()=>M;return fe.d(R,{a:R}),R},fe.d=(M,R)=>{for(var J in R)fe.o(R,J)&&!fe.o(M,J)&&Object.defineProperty(M,J,{enumerable:!0,get:R[J]})},fe.o=(M,R)=>Object.prototype.hasOwnProperty.call(M,R),fe.nc=void 0;var lc={};(()=>{"use strict";var En;var M=fe(85072),R=fe.n(M),J=fe(2410),oe={};oe.insert="head",oe.singleton=!1;var le=R()(J.A,oe);const I=J.A.locals||{};var y=fe(3554),v={};v.insert="head",v.singleton=!1;var H=R()(y.A,v);const z=y.A.locals||{};var W=fe(17334),l=fe(96540),ae=fe(40961),G=(o=>(o[o.Committed=0]="Committed",o[o.Mentioned=1]="Mentioned",o[o.Subscribed=2]="Subscribed",o[o.Commented=3]="Commented",o[o.Reviewed=4]="Reviewed",o[o.NewCommitsSinceReview=5]="NewCommitsSinceReview",o[o.Labeled=6]="Labeled",o[o.Milestoned=7]="Milestoned",o[o.Assigned=8]="Assigned",o[o.Unassigned=9]="Unassigned",o[o.HeadRefDeleted=10]="HeadRefDeleted",o[o.Merged=11]="Merged",o[o.CrossReferenced=12]="CrossReferenced",o[o.Closed=13]="Closed",o[o.Reopened=14]="Reopened",o[o.CopilotStarted=15]="CopilotStarted",o[o.CopilotFinished=16]="CopilotFinished",o[o.CopilotFinishedError=17]="CopilotFinishedError",o[o.Other=18]="Other",o))(G||{}),Oe=Object.defineProperty,De=i((o,a,u)=>a in o?Oe(o,a,{enumerable:!0,configurable:!0,writable:!0,value:u}):o[a]=u,"__defNormalProp"),$=i((o,a,u)=>De(o,typeof a!="symbol"?a+"":a,u),"__publicField");const Z=acquireVsCodeApi(),zo=class zo{constructor(a){$(this,"_commandHandler"),$(this,"lastSentReq"),$(this,"pendingReplies"),this._commandHandler=a,this.lastSentReq=0,this.pendingReplies=Object.create(null),window.addEventListener("message",this.handleMessage.bind(this))}registerCommandHandler(a){this._commandHandler=a}async postMessage(a){const u=String(++this.lastSentReq);return new Promise((d,f)=>{this.pendingReplies[u]={resolve:d,reject:f},a=Object.assign(a,{req:u}),Z.postMessage(a)})}handleMessage(a){const u=a.data;if(u.seq){const d=this.pendingReplies[u.seq];if(d){u.err?d.reject(u.err):d.resolve(u.res);return}}this._commandHandler&&this._commandHandler(u.res)}};i(zo,"MessageHandler");let me=zo;function P(o){return new me(o)}i(P,"getMessageHandler");function _(){return Z.getState()}i(_,"getState");function T(o){const a=_();a&&a.number&&a.number===o.number&&(o.pendingCommentText=a.pendingCommentText),o&&Z.setState(o)}i(T,"setState");function q(o){const a=Z.getState();Z.setState(Object.assign(a,o))}i(q,"updateState");var ee=Object.defineProperty,V=i((o,a,u)=>a in o?ee(o,a,{enumerable:!0,configurable:!0,writable:!0,value:u}):o[a]=u,"context_defNormalProp"),E=i((o,a,u)=>V(o,typeof a!="symbol"?a+"":a,u),"context_publicField");const A=(En=class{constructor(a=_(),u=null,d=null){this.pr=a,this.onchange=u,this._handler=d,E(this,"setTitle",async f=>{const p=await this.postMessage({command:"pr.edit-title",args:{text:f}});this.updatePR({titleHTML:p.titleHTML})}),E(this,"setDescription",f=>this.postMessage({command:"pr.edit-description",args:{text:f}})),E(this,"checkout",()=>this.postMessage({command:"pr.checkout"})),E(this,"openChanges",f=>this.postMessage({command:"pr.open-changes",args:{openToTheSide:f}})),E(this,"copyPrLink",()=>this.postMessage({command:"pr.copy-prlink"})),E(this,"copyVscodeDevLink",()=>this.postMessage({command:"pr.copy-vscodedevlink"})),E(this,"cancelCodingAgent",f=>this.postMessage({command:"pr.cancel-coding-agent",args:f})),E(this,"exitReviewMode",async()=>{if(this.pr)return this.postMessage({command:"pr.checkout-default-branch",args:this.pr.repositoryDefaultBranch})}),E(this,"gotoChangesSinceReview",()=>this.postMessage({command:"pr.gotoChangesSinceReview"})),E(this,"refresh",()=>this.postMessage({command:"pr.refresh"})),E(this,"checkMergeability",()=>this.postMessage({command:"pr.checkMergeability"})),E(this,"changeEmail",async f=>{const p=await this.postMessage({command:"pr.change-email",args:f});this.updatePR({emailForCommit:p})}),E(this,"merge",async f=>await this.postMessage({command:"pr.merge",args:f})),E(this,"openOnGitHub",()=>this.postMessage({command:"pr.openOnGitHub"})),E(this,"deleteBranch",()=>this.postMessage({command:"pr.deleteBranch"})),E(this,"revert",async()=>{this.updatePR({busy:!0});const f=await this.postMessage({command:"pr.revert"});this.updatePR({busy:!1,...f})}),E(this,"readyForReview",()=>this.postMessage({command:"pr.readyForReview"})),E(this,"addReviewers",()=>this.postMessage({command:"pr.change-reviewers"})),E(this,"changeProjects",()=>this.postMessage({command:"pr.change-projects"})),E(this,"removeProject",f=>this.postMessage({command:"pr.remove-project",args:f})),E(this,"addMilestone",()=>this.postMessage({command:"pr.add-milestone"})),E(this,"removeMilestone",()=>this.postMessage({command:"pr.remove-milestone"})),E(this,"addAssignees",()=>this.postMessage({command:"pr.change-assignees"})),E(this,"addAssigneeYourself",()=>this.postMessage({command:"pr.add-assignee-yourself"})),E(this,"addAssigneeCopilot",()=>this.postMessage({command:"pr.add-assignee-copilot"})),E(this,"addLabels",()=>this.postMessage({command:"pr.add-labels"})),E(this,"create",()=>this.postMessage({command:"pr.open-create"})),E(this,"deleteComment",async f=>{await this.postMessage({command:"pr.delete-comment",args:f});const{pr:p}=this,{id:g,pullRequestReviewId:C}=f;if(!C){this.updatePR({events:p.events.filter(K=>K.id!==g)});return}const k=p.events.findIndex(K=>K.id===C);if(k===-1){console.error("Could not find review:",C);return}const D=p.events[k];if(!D.comments){console.error("No comments to delete for review:",C,D);return}this.pr.events.splice(k,1,{...D,comments:D.comments.filter(K=>K.id!==g)}),this.updatePR(this.pr)}),E(this,"editComment",f=>this.postMessage({command:"pr.edit-comment",args:f})),E(this,"updateDraft",(f,p)=>{const C=_().pendingCommentDrafts||Object.create(null);p!==C[f]&&(C[f]=p,this.updatePR({pendingCommentDrafts:C}))}),E(this,"requestChanges",f=>this.submitReviewCommand("pr.request-changes",f)),E(this,"approve",f=>this.submitReviewCommand("pr.approve",f)),E(this,"submit",f=>this.submitReviewCommand("pr.submit",f)),E(this,"close",async f=>{try{const p=await this.postMessage({command:"pr.close",args:f});let g=[...this.pr.events];p.commentEvent&&g.push(p.commentEvent),p.closeEvent&&g.push(p.closeEvent),this.updatePR({events:g,pendingCommentText:"",state:p.state})}catch{}}),E(this,"removeLabel",async f=>{await this.postMessage({command:"pr.remove-label",args:f});const p=this.pr.labels.filter(g=>g.name!==f);this.updatePR({labels:p})}),E(this,"applyPatch",async f=>{this.postMessage({command:"pr.apply-patch",args:{comment:f}})}),E(this,"reRequestReview",async f=>{const{reviewers:p}=await this.postMessage({command:"pr.re-request-review",args:f}),g=this.pr;g.reviewers=p,this.updatePR(g)}),E(this,"updateBranch",async()=>{var f,p;const g=await this.postMessage({command:"pr.update-branch"}),C=this.pr;C.events=(f=g.events)!=null?f:C.events,C.mergeable=(p=g.mergeable)!=null?p:C.mergeable,this.updatePR(C)}),E(this,"dequeue",async()=>{const f=await this.postMessage({command:"pr.dequeue"}),p=this.pr;f&&(p.mergeQueueEntry=void 0),this.updatePR(p)}),E(this,"enqueue",async()=>{const f=await this.postMessage({command:"pr.enqueue"}),p=this.pr;f.mergeQueueEntry&&(p.mergeQueueEntry=f.mergeQueueEntry),this.updatePR(p)}),E(this,"openDiff",f=>this.postMessage({command:"pr.open-diff",args:{comment:f}})),E(this,"toggleResolveComment",(f,p,g)=>{this.postMessage({command:"pr.resolve-comment-thread",args:{threadId:f,toResolve:g,thread:p}}).then(C=>{C?this.updatePR({events:C}):this.refresh()})}),E(this,"openSessionLog",(f,p)=>this.postMessage({command:"pr.open-session-log",args:{link:f,openToTheSide:p}})),E(this,"setPR",f=>(this.pr=f,T(this.pr),this.onchange&&this.onchange(this.pr),this)),E(this,"updatePR",f=>(q(f),this.pr={...this.pr,...f},this.onchange&&this.onchange(this.pr),this)),E(this,"handleMessage",f=>{var p;switch(f.command){case"pr.initialize":return this.setPR(f.pullrequest);case"update-state":return this.updatePR({state:f.state});case"pr.update-checkout-status":return this.updatePR({isCurrentlyCheckedOut:f.isCurrentlyCheckedOut});case"pr.deleteBranch":const g={};return f.branchTypes&&f.branchTypes.map(k=>{k==="local"?g.isLocalHeadDeleted=!0:(k==="remote"||k==="upstream")&&(g.isRemoteHeadDeleted=!0)}),this.updatePR(g);case"pr.enable-exit":return this.updatePR({isCurrentlyCheckedOut:!0});case"set-scroll":window.scrollTo(f.scrollPosition.x,f.scrollPosition.y);return;case"pr.scrollToPendingReview":const C=(p=document.getElementById("pending-review"))!=null?p:document.getElementById("comment-textarea");C&&(C.scrollIntoView(),C.focus());return;case"pr.submitting-review":return this.updatePR({busy:!0,lastReviewType:f.lastReviewType});case"pr.append-review":return this.appendReview(f)}}),d||(this._handler=P(this.handleMessage))}async submitReviewCommand(a,u){try{const d=await this.postMessage({command:a,args:u});return this.appendReview(d)}catch{return this.updatePR({busy:!1})}}appendReview(a){const{events:u,reviewers:d,reviewedEvent:f}=a,p=this.pr;if(p.busy=!1,!u){this.updatePR(p);return}d&&(p.reviewers=d),p.events=u.length===0?[...p.events,f]:u,f.event===G.Reviewed&&(p.currentUserReviewState=f.state),p.pendingCommentText="",p.pendingReviewType=void 0,this.updatePR(p)}async updateAutoMerge({autoMerge:a,autoMergeMethod:u}){const d=await this.postMessage({command:"pr.update-automerge",args:{autoMerge:a,autoMergeMethod:u}}),f=this.pr;f.autoMerge=d.autoMerge,f.autoMergeMethod=d.autoMergeMethod,this.updatePR(f)}postMessage(a){var u,d;return(d=(u=this._handler)==null?void 0:u.postMessage(a))!=null?d:Promise.resolve(void 0)}},i(En,"_PRContext"),En);E(A,"instance",new A);let ie=A;const B=(0,l.createContext)(ie.instance);var ge=(o=>(o[o.Query=0]="Query",o[o.All=1]="All",o[o.LocalPullRequest=2]="LocalPullRequest",o))(ge||{}),ve=(o=>(o.Approve="APPROVE",o.RequestChanges="REQUEST_CHANGES",o.Comment="COMMENT",o))(ve||{}),de=(o=>(o.Open="OPEN",o.Merged="MERGED",o.Closed="CLOSED",o))(de||{}),Ce=(o=>(o[o.Mergeable=0]="Mergeable",o[o.NotMergeable=1]="NotMergeable",o[o.Conflict=2]="Conflict",o[o.Unknown=3]="Unknown",o[o.Behind=4]="Behind",o))(Ce||{}),Te=(o=>(o[o.AwaitingChecks=0]="AwaitingChecks",o[o.Locked=1]="Locked",o[o.Mergeable=2]="Mergeable",o[o.Queued=3]="Queued",o[o.Unmergeable=4]="Unmergeable",o))(Te||{}),Ze=(o=>(o.User="User",o.Organization="Organization",o.Mannequin="Mannequin",o.Bot="Bot",o))(Ze||{});function Qe(o){switch(o){case"Organization":return"Organization";case"Mannequin":return"Mannequin";case"Bot":return"Bot";default:return"User"}}i(Qe,"toAccountType");function nt(o){var a;return ot(o)?o.id:(a=o.specialDisplayName)!=null?a:o.login}i(nt,"reviewerId");function st(o){var a,u,d;return ot(o)?(u=(a=o.name)!=null?a:o.slug)!=null?u:o.id:(d=o.specialDisplayName)!=null?d:o.login}i(st,"reviewerLabel");function ot(o){return"org"in o}i(ot,"isTeam");function Fe(o){return"isAuthor"in o&&"isCommenter"in o}i(Fe,"isSuggestedReviewer");var F=(o=>(o.Issue="Issue",o.PullRequest="PullRequest",o))(F||{}),U=(o=>(o.Success="success",o.Failure="failure",o.Neutral="neutral",o.Pending="pending",o.Unknown="unknown",o))(U||{}),te=(o=>(o.Comment="comment",o.Approve="approve",o.RequestChanges="requestChanges",o))(te||{}),w=(o=>(o[o.None=0]="None",o[o.Available=1]="Available",o[o.ReviewedWithComments=2]="ReviewedWithComments",o[o.ReviewedWithoutComments=3]="ReviewedWithoutComments",o))(w||{});function O(o){var a,u;const d=(a=o.submittedAt)!=null?a:o.createdAt,f=d&&Date.now()-new Date(d).getTime()<1e3*60,p=(u=o.state)!=null?u:o.event===G.Commented?"COMMENTED":void 0;let g="";if(f)switch(p){case"APPROVED":g="Pull request approved";break;case"CHANGES_REQUESTED":g="Changes requested on pull request";break;case"COMMENTED":g="Commented on pull request";break}return g}i(O,"ariaAnnouncementForReview");var he=fe(37007);const Ee=new he.EventEmitter;function we(o){const[a,u]=(0,l.useState)(o);return(0,l.useEffect)(()=>{a!==o&&u(o)},[o]),[a,u]}i(we,"useStateProp");const se=i(({className:o="",src:a,title:u})=>l.createElement("span",{className:`icon ${o}`,title:u,dangerouslySetInnerHTML:{__html:a}}),"Icon"),pt=null,ke=l.createElement(se,{src:fe(61440)}),Se=l.createElement(se,{src:fe(34894),className:"check"}),ht=l.createElement(se,{src:fe(61779),className:"skip"}),jr=l.createElement(se,{src:fe(30407)}),xt=l.createElement(se,{src:fe(10650)}),Br=l.createElement(se,{src:fe(2301)}),Hl=l.createElement(se,{src:fe(72362)}),dt=l.createElement(se,{src:fe(5771)}),Ur=l.createElement(se,{src:fe(37165)}),cn=l.createElement(se,{src:fe(46279)}),Pt=l.createElement(se,{src:fe(90346)}),Fl=l.createElement(se,{src:fe(44370)}),fr=l.createElement(se,{src:fe(90659)}),si=l.createElement(se,{src:fe(14268)}),ai=l.createElement(se,{src:fe(83344)}),zl=l.createElement(se,{src:fe(83962)}),Wr=l.createElement(se,{src:fe(15010)}),dn=l.createElement(se,{src:fe(19443),className:"pending"}),mr=l.createElement(se,{src:fe(98923)}),en=l.createElement(se,{src:fe(15493)}),Ot=l.createElement(se,{src:fe(85130),className:"close"}),ui=l.createElement(se,{src:fe(17411)}),Vl=l.createElement(se,{src:fe(30340)}),pr=l.createElement(se,{src:fe(9649)}),pa=l.createElement(se,{src:fe(92359)}),jt=l.createElement(se,{src:fe(34439)}),ci=l.createElement(se,{src:fe(96855)}),tn=l.createElement(se,{src:fe(5064)}),$l=l.createElement(se,{src:fe(20628)}),ha=l.createElement(se,{src:fe(80459)}),qr=l.createElement(se,{src:fe(70596)}),jl=l.createElement(se,{src:fe(33027)}),Zr=l.createElement(se,{src:fe(40027)}),di=l.createElement(se,{src:fe(64674)}),fi=l.createElement(se,{src:fe(12158)}),Qr=l.createElement(se,{src:fe(2481)}),mi=l.createElement(se,{src:fe(65013)}),Kr=l.createElement(se,{src:fe(93492)});function Bl(){const[o,a]=(0,l.useState)([0,0]);return(0,l.useLayoutEffect)(()=>{function u(){a([window.innerWidth,window.innerHeight])}return i(u,"updateSize"),window.addEventListener("resize",u),u(),()=>window.removeEventListener("resize",u)},[]),o}i(Bl,"useWindowSize");const Yr=i(({optionsContext:o,defaultOptionLabel:a,defaultOptionValue:u,defaultAction:d,allOptions:f,optionsTitle:p,disabled:g,hasSingleAction:C})=>{const[k,D]=(0,l.useState)(!1),K=i(ne=>{ne.target instanceof HTMLElement&&ne.target.classList.contains("split-right")||D(!1)},"onHideAction");(0,l.useEffect)(()=>{const ne=i(Ie=>K(Ie),"onClickOrKey");k?(document.addEventListener("click",ne),document.addEventListener("keydown",ne)):(document.removeEventListener("click",ne),document.removeEventListener("keydown",ne))},[k,D]);const Y=(0,l.useRef)();return Bl(),l.createElement("div",{className:"dropdown-container",ref:Y},Y.current&&Y.current.clientWidth>375&&f&&!C?f().map(({label:ne,value:Ie,action:$e})=>l.createElement("button",{className:"inlined-dropdown",key:Ie,title:ne,disabled:g,onClick:$e,value:Ie},ne)):l.createElement("div",{className:"primary-split-button"},l.createElement("button",{className:"split-left",disabled:g,onClick:d,value:u(),title:a()},a()),l.createElement("div",{className:"split"}),C?null:l.createElement("button",{className:"split-right",title:p,disabled:g,"aria-expanded":k,onClick:i(ne=>{ne.preventDefault();const Ie=ne.target.getBoundingClientRect(),$e=Ie.left,Be=Ie.bottom;ne.target.dispatchEvent(new MouseEvent("contextmenu",{bubbles:!0,clientX:$e,clientY:Be})),ne.stopPropagation()},"onClick"),onMouseDown:i(()=>D(!0),"onMouseDown"),onKeyDown:i(ne=>{(ne.key==="Enter"||ne.key===" ")&&D(!0)},"onKeyDown"),"data-vscode-context":o()},xt)))},"contextDropdown_ContextDropdown"),Ue="\xA0",Gr=i(({children:o})=>{const a=l.Children.count(o);return l.createElement(l.Fragment,{children:l.Children.map(o,(u,d)=>typeof u=="string"?`${d>0?Ue:""}${u}${d<a-1&&typeof o[d+1]!="string"?Ue:""}`:u)})},"Spaced");var Ul=fe(57975),pi=fe(74353),On=fe.n(pi),hi=fe(6279),vi=fe.n(hi),Xr=fe(53581),hr=fe.n(Xr),gi=Object.defineProperty,Dn=i((o,a,u)=>a in o?gi(o,a,{enumerable:!0,configurable:!0,writable:!0,value:u}):o[a]=u,"lifecycle_defNormalProp"),An=i((o,a,u)=>Dn(o,typeof a!="symbol"?a+"":a,u),"lifecycle_publicField");function fn(o){return{dispose:o}}i(fn,"toDisposable");function yi(o){return fn(()=>Jr(o))}i(yi,"lifecycle_combinedDisposable");function Jr(o){for(;o.length;){const a=o.pop();a==null||a.dispose()}}i(Jr,"disposeAll");function eo(o,a){return a.push(o),o}i(eo,"addDisposable");const Vo=class Vo{constructor(){An(this,"_isDisposed",!1),An(this,"_disposables",[])}dispose(){this._isDisposed||(this._isDisposed=!0,Jr(this._disposables),this._disposables=[])}_register(a){return this._isDisposed?a.dispose():this._disposables.push(a),a}get isDisposed(){return this._isDisposed}};i(Vo,"Disposable");let to=Vo;var Ci=Object.defineProperty,wi=i((o,a,u)=>a in o?Ci(o,a,{enumerable:!0,configurable:!0,writable:!0,value:u}):o[a]=u,"utils_defNormalProp"),Ke=i((o,a,u)=>wi(o,typeof a!="symbol"?a+"":a,u),"utils_publicField");On().extend(vi(),{thresholds:[{l:"s",r:44,d:"second"},{l:"m",r:89},{l:"mm",r:44,d:"minute"},{l:"h",r:89},{l:"hh",r:21,d:"hour"},{l:"d",r:35},{l:"dd",r:6,d:"day"},{l:"w",r:7},{l:"ww",r:3,d:"week"},{l:"M",r:4},{l:"MM",r:10,d:"month"},{l:"y",r:17},{l:"yy",d:"year"}]}),On().extend(hr()),On().updateLocale("en",{relativeTime:{future:"in %s",past:"%s ago",s:"seconds",m:"a minute",mm:"%d minutes",h:"an hour",hh:"%d hours",d:"a day",dd:"%d days",w:"a week",ww:"%d weeks",M:"a month",MM:"%d months",y:"a year",yy:"%d years"}});function vr(o,a){const u=Object.create(null);return o.filter(d=>{const f=a(d);return u[f]?!1:(u[f]=!0,!0)})}i(vr,"uniqBy");function Wl(...o){return(a,u=null,d)=>{const f=combinedDisposable(o.map(p=>p(g=>a.call(u,g))));return d&&d.push(f),f}}i(Wl,"anyEvent");function xi(o,a){return(u,d=null,f)=>o(p=>a(p)&&u.call(d,p),null,f)}i(xi,"filterEvent");function mn(o){return(a,u=null,d)=>{const f=o(p=>(f.dispose(),a.call(u,p)),null,d);return f}}i(mn,"onceEvent");function Ei(o){return/^[a-zA-Z]:\\/.test(o)}i(Ei,"isWindowsPath");function ql(o,a,u=sep){return o===a?!0:(o.charAt(o.length-1)!==u&&(o+=u),Ei(o)&&(o=o.toLowerCase(),a=a.toLowerCase()),a.startsWith(o))}i(ql,"isDescendant");function ki(o,a){return o.reduce((u,d)=>{const f=a(d);return u[f]=[...u[f]||[],d],u},Object.create(null))}i(ki,"groupBy");const $o=class $o extends Error{constructor(a){super(`Unreachable case: ${a}`)}};i($o,"UnreachableCaseError");let gr=$o;function pn(o){return!!o.errors}i(pn,"isHookError");function no(o){let a=!0;if(o.errors&&Array.isArray(o.errors)){for(const u of o.errors)if(!u.field||!u.value||!u.status){a=!1;break}}else a=!1;return a}i(no,"hasFieldErrors");function yr(o){if(!(o instanceof Error))return typeof o=="string"?o:o.gitErrorCode?`${o.message}. Please check git output for more details`:o.stderr?`${o.stderr}. Please check git output for more details`:"Error";let a=o.message,u;if(o.message==="Validation Failed"&&no(o))u=o.errors.map(d=>`Value "${d.value}" cannot be set for field ${d.field} (code: ${d.status})`).join(", ");else{if(o.message.startsWith("Validation Failed:"))return o.message;if(pn(o)&&o.errors)return o.errors.map(d=>typeof d=="string"?d:d.message).join(", ")}return u&&(a=`${a}: ${u}`),a}i(yr,"formatError");async function va(o){return new Promise(a=>{const u=o(d=>{u.dispose(),a(d)})})}i(va,"asPromise");async function ro(o,a){return Promise.race([o,new Promise(u=>{setTimeout(()=>u(void 0),a)})])}i(ro,"promiseWithTimeout");function Cr(o){const a=On()(o),u=Date.now();return a.diff(u,"month"),a.diff(u,"month")<1?a.fromNow():a.diff(u,"year")<1?`on ${a.format("MMM D")}`:`on ${a.format("MMM D, YYYY")}`}i(Cr,"dateFromNow");function wr(o,a,u=!1){o.startsWith("#")&&(o=o.substring(1));const d=oo(o);if(a){const f=bi(d.r,d.g,d.b),p=.6,g=.18,C=.3,k=(d.r*.2126+d.g*.7152+d.b*.0722)/255,D=Math.max(0,Math.min((k-p)*-1e3,1)),K=(p-k)*100*D,Y=oo(io(f.h,f.s,f.l+K)),ne=`#${io(f.h,f.s,f.l+K)}`,Ie=u?`#${In({...d,a:g})}`:`rgba(${d.r},${d.g},${d.b},${g})`,$e=u?`#${In({...Y,a:C})}`:`rgba(${Y.r},${Y.g},${Y.b},${C})`;return{textColor:ne,backgroundColor:Ie,borderColor:$e}}else return{textColor:`#${lo(d)}`,backgroundColor:`#${o}`,borderColor:`#${o}`}}i(wr,"utils_gitHubLabelColor");const In=i(o=>{const a=[o.r,o.g,o.b];return o.a&&a.push(Math.floor(o.a*255)),a.map(u=>u.toString(16).padStart(2,"0")).join("")},"rgbToHex");function oo(o){const a=/^([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(o);return a?{r:parseInt(a[1],16),g:parseInt(a[2],16),b:parseInt(a[3],16)}:{r:0,g:0,b:0}}i(oo,"hexToRgb");function bi(o,a,u){o/=255,a/=255,u/=255;let d=Math.min(o,a,u),f=Math.max(o,a,u),p=f-d,g=0,C=0,k=0;return p==0?g=0:f==o?g=(a-u)/p%6:f==a?g=(u-o)/p+2:g=(o-a)/p+4,g=Math.round(g*60),g<0&&(g+=360),k=(f+d)/2,C=p==0?0:p/(1-Math.abs(2*k-1)),C=+(C*100).toFixed(1),k=+(k*100).toFixed(1),{h:g,s:C,l:k}}i(bi,"rgbToHsl");function io(o,a,u){const d=u/100,f=a*Math.min(d,1-d)/100,p=i(g=>{const C=(g+o/30)%12,k=d-f*Math.max(Math.min(C-3,9-C,1),-1);return Math.round(255*k).toString(16).padStart(2,"0")},"f");return`${p(0)}${p(8)}${p(4)}`}i(io,"hslToHex");function lo(o){return(.299*o.r+.587*o.g+.114*o.b)/255>.5?"000000":"ffffff"}i(lo,"contrastColor");var _i=(o=>(o[o.Period=46]="Period",o[o.Slash=47]="Slash",o[o.A=65]="A",o[o.Z=90]="Z",o[o.Backslash=92]="Backslash",o[o.a=97]="a",o[o.z=122]="z",o))(_i||{});function xr(o,a){return o<a?-1:o>a?1:0}i(xr,"compare");function Er(o,a,u=0,d=o.length,f=0,p=a.length){for(;u<d&&f<p;u++,f++){const k=o.charCodeAt(u),D=a.charCodeAt(f);if(k<D)return-1;if(k>D)return 1}const g=d-u,C=p-f;return g<C?-1:g>C?1:0}i(Er,"compareSubstring");function kr(o,a){return gt(o,a,0,o.length,0,a.length)}i(kr,"compareIgnoreCase");function gt(o,a,u=0,d=o.length,f=0,p=a.length){for(;u<d&&f<p;u++,f++){let k=o.charCodeAt(u),D=a.charCodeAt(f);if(k===D)continue;const K=k-D;if(!(K===32&&At(D))&&!(K===-32&&At(k)))return Dt(k)&&Dt(D)?K:Er(o.toLowerCase(),a.toLowerCase(),u,d,f,p)}const g=d-u,C=p-f;return g<C?-1:g>C?1:0}i(gt,"compareSubstringIgnoreCase");function Dt(o){return o>=97&&o<=122}i(Dt,"isLowerAsciiLetter");function At(o){return o>=65&&o<=90}i(At,"isUpperAsciiLetter");const jo=class jo{constructor(){Ke(this,"_value",""),Ke(this,"_pos",0)}reset(a){return this._value=a,this._pos=0,this}next(){return this._pos+=1,this}hasNext(){return this._pos<this._value.length-1}cmp(a){const u=a.charCodeAt(0),d=this._value.charCodeAt(this._pos);return u-d}value(){return this._value[this._pos]}};i(jo,"StringIterator");let St=jo;const Bo=class Bo{constructor(a=!0){this._caseSensitive=a,Ke(this,"_value"),Ke(this,"_from"),Ke(this,"_to")}reset(a){return this._value=a,this._from=0,this._to=0,this.next()}hasNext(){return this._to<this._value.length}next(){this._from=this._to;let a=!0;for(;this._to<this._value.length;this._to++)if(this._value.charCodeAt(this._to)===46)if(a)this._from++;else break;else a=!1;return this}cmp(a){return this._caseSensitive?Er(a,this._value,0,a.length,this._from,this._to):gt(a,this._value,0,a.length,this._from,this._to)}value(){return this._value.substring(this._from,this._to)}};i(Bo,"ConfigKeysIterator");let nn=Bo;const Uo=class Uo{constructor(a=!0,u=!0){this._splitOnBackslash=a,this._caseSensitive=u,Ke(this,"_value"),Ke(this,"_from"),Ke(this,"_to")}reset(a){return this._value=a.replace(/\\$|\/$/,""),this._from=0,this._to=0,this.next()}hasNext(){return this._to<this._value.length}next(){this._from=this._to;let a=!0;for(;this._to<this._value.length;this._to++){const u=this._value.charCodeAt(this._to);if(u===47||this._splitOnBackslash&&u===92)if(a)this._from++;else break;else a=!1}return this}cmp(a){return this._caseSensitive?Er(a,this._value,0,a.length,this._from,this._to):gt(a,this._value,0,a.length,this._from,this._to)}value(){return this._value.substring(this._from,this._to)}};i(Uo,"PathIterator");let Bt=Uo;var Hn=(o=>(o[o.Scheme=1]="Scheme",o[o.Authority=2]="Authority",o[o.Path=3]="Path",o[o.Query=4]="Query",o[o.Fragment=5]="Fragment",o))(Hn||{});const Wo=class Wo{constructor(a){this._ignorePathCasing=a,Ke(this,"_pathIterator"),Ke(this,"_value"),Ke(this,"_states",[]),Ke(this,"_stateIdx",0)}reset(a){return this._value=a,this._states=[],this._value.scheme&&this._states.push(1),this._value.authority&&this._states.push(2),this._value.path&&(this._pathIterator=new Bt(!1,!this._ignorePathCasing(a)),this._pathIterator.reset(a.path),this._pathIterator.value()&&this._states.push(3)),this._value.query&&this._states.push(4),this._value.fragment&&this._states.push(5),this._stateIdx=0,this}next(){return this._states[this._stateIdx]===3&&this._pathIterator.hasNext()?this._pathIterator.next():this._stateIdx+=1,this}hasNext(){return this._states[this._stateIdx]===3&&this._pathIterator.hasNext()||this._stateIdx<this._states.length-1}cmp(a){if(this._states[this._stateIdx]===1)return kr(a,this._value.scheme);if(this._states[this._stateIdx]===2)return kr(a,this._value.authority);if(this._states[this._stateIdx]===3)return this._pathIterator.cmp(a);if(this._states[this._stateIdx]===4)return xr(a,this._value.query);if(this._states[this._stateIdx]===5)return xr(a,this._value.fragment);throw new Error}value(){if(this._states[this._stateIdx]===1)return this._value.scheme;if(this._states[this._stateIdx]===2)return this._value.authority;if(this._states[this._stateIdx]===3)return this._pathIterator.value();if(this._states[this._stateIdx]===4)return this._value.query;if(this._states[this._stateIdx]===5)return this._value.fragment;throw new Error}};i(Wo,"UriIterator");let Fn=Wo;function ga(o){const u=o.extensionUri.path,d=u.lastIndexOf(".");return d===-1?!1:u.substr(d+1).length>1}i(ga,"isPreRelease");const qo=class qo{constructor(){Ke(this,"segment"),Ke(this,"value"),Ke(this,"key"),Ke(this,"left"),Ke(this,"mid"),Ke(this,"right")}isEmpty(){return!this.left&&!this.mid&&!this.right&&!this.value}};i(qo,"TernarySearchTreeNode");let hn=qo;const zt=class zt{constructor(a){Ke(this,"_iter"),Ke(this,"_root"),this._iter=a}static forUris(a=()=>!1){return new zt(new Fn(a))}static forPaths(){return new zt(new Bt)}static forStrings(){return new zt(new St)}static forConfigKeys(){return new zt(new nn)}clear(){this._root=void 0}set(a,u){const d=this._iter.reset(a);let f;for(this._root||(this._root=new hn,this._root.segment=d.value()),f=this._root;;){const g=d.cmp(f.segment);if(g>0)f.left||(f.left=new hn,f.left.segment=d.value()),f=f.left;else if(g<0)f.right||(f.right=new hn,f.right.segment=d.value()),f=f.right;else if(d.hasNext())d.next(),f.mid||(f.mid=new hn,f.mid.segment=d.value()),f=f.mid;else break}const p=f.value;return f.value=u,f.key=a,p}get(a){var u;return(u=this._getNode(a))==null?void 0:u.value}_getNode(a){const u=this._iter.reset(a);let d=this._root;for(;d;){const f=u.cmp(d.segment);if(f>0)d=d.left;else if(f<0)d=d.right;else if(u.hasNext())u.next(),d=d.mid;else break}return d}has(a){const u=this._getNode(a);return!((u==null?void 0:u.value)===void 0&&(u==null?void 0:u.mid)===void 0)}delete(a){return this._delete(a,!1)}deleteSuperstr(a){return this._delete(a,!0)}_delete(a,u){const d=this._iter.reset(a),f=[];let p=this._root;for(;p;){const g=d.cmp(p.segment);if(g>0)f.push([1,p]),p=p.left;else if(g<0)f.push([-1,p]),p=p.right;else if(d.hasNext())d.next(),f.push([0,p]),p=p.mid;else{for(u?(p.left=void 0,p.mid=void 0,p.right=void 0):p.value=void 0;f.length>0&&p.isEmpty();){let[C,k]=f.pop();switch(C){case 1:k.left=void 0;break;case 0:k.mid=void 0;break;case-1:k.right=void 0;break}p=k}break}}}findSubstr(a){const u=this._iter.reset(a);let d=this._root,f;for(;d;){const p=u.cmp(d.segment);if(p>0)d=d.left;else if(p<0)d=d.right;else if(u.hasNext())u.next(),f=d.value||f,d=d.mid;else break}return d&&d.value||f}findSuperstr(a){const u=this._iter.reset(a);let d=this._root;for(;d;){const f=u.cmp(d.segment);if(f>0)d=d.left;else if(f<0)d=d.right;else if(u.hasNext())u.next(),d=d.mid;else return d.mid?this._entries(d.mid):void 0}}forEach(a){for(const[u,d]of this)a(d,u)}*[Symbol.iterator](){yield*this._entries(this._root)}*_entries(a){a&&(yield*this._entries(a.left),a.value&&(yield[a.key,a.value]),yield*this._entries(a.mid),yield*this._entries(a.right))}};i(zt,"TernarySearchTree");let br=zt;async function Zl(o,a,u){const d=[];o.replace(a,(g,...C)=>{const k=u(g,...C);return d.push(k),""});const f=await Promise.all(d);let p=0;return o.replace(a,()=>f[p++])}i(Zl,"stringReplaceAsync");async function _r(o,a,u){const d=Math.ceil(o.length/a);for(let f=0;f<d;f++){const p=o.slice(f*a,(f+1)*a);await Promise.all(p.map(u))}}i(_r,"batchPromiseAll");function ya(o){return o.replace(/[.*+?^${}()|[\]\\]/g,"\\$&")}i(ya,"escapeRegExp");const Et=i(({date:o,href:a})=>{const u=typeof o=="string"?new Date(o).toLocaleString():o.toLocaleString();return a?l.createElement("a",{href:a,className:"timestamp",title:u},Cr(o)):l.createElement("div",{className:"timestamp",title:u},Cr(o))},"Timestamp"),so=null,ao=i(({for:o})=>l.createElement(l.Fragment,null,o.avatarUrl?l.createElement("img",{className:"avatar",src:o.avatarUrl,alt:"",role:"presentation"}):l.createElement(se,{className:"avatar-icon",src:fe(38440)})),"InnerAvatar"),yt=i(({for:o,link:a=!0})=>a?l.createElement("a",{className:"avatar-link",href:o.url,title:o.url},l.createElement(ao,{for:o})):l.createElement(ao,{for:o}),"Avatar"),Xe=i(({for:o,text:a=st(o)})=>l.createElement("a",{className:"author-link",href:o.url,"aria-label":a,title:o.url},a),"AuthorLink"),Li=i(({authorAssociation:o},a=u=>`(${u.toLowerCase()})`)=>o.toLowerCase()==="user"?a("you"):o&&o!=="NONE"?a(o):null,"association");function zn(o){const{isPRDescription:a,children:u,comment:d,headerInEditMode:f}=o,{bodyHTML:p,body:g}=d,C="id"in d?d.id:-1,k="canEdit"in d?d.canEdit:!1,D="canDelete"in d?d.canDelete:!1,K=d.pullRequestReviewId,[Y,ne]=we(g),[Ie,$e]=we(p),{deleteComment:Be,editComment:be,setDescription:Re,pr:He}=(0,l.useContext)(B),et=He.pendingCommentDrafts&&He.pendingCommentDrafts[C],[at,tt]=(0,l.useState)(!!et),[Ne,Zt]=(0,l.useState)(!1);if(at)return l.cloneElement(f?l.createElement(fo,{for:d}):l.createElement(l.Fragment,null),{},[l.createElement(mo,{id:C,key:`editComment${C}`,body:et||Y,onCancel:i(()=>{He.pendingCommentDrafts&&delete He.pendingCommentDrafts[C],tt(!1)},"onCancel"),onSave:i(async ze=>{try{const ut=a?await Re(ze):await be({comment:d,text:ze});$e(ut.bodyHTML),ne(ze)}finally{tt(!1)}},"onSave")})]);const qe=d.event===G.Commented||d.event===G.Reviewed?O(d):void 0;return l.createElement(fo,{for:d,onMouseEnter:i(()=>Zt(!0),"onMouseEnter"),onMouseLeave:i(()=>Zt(!1),"onMouseLeave"),onFocus:i(()=>Zt(!0),"onFocus")},qe?l.createElement("div",{role:"alert","aria-label":qe}):null,l.createElement("div",{className:"action-bar comment-actions",style:{display:Ne?"flex":"none"}},l.createElement("button",{title:"Quote reply",className:"icon-button",onClick:i(()=>Ee.emit("quoteReply",Y),"onClick")},Hl),k?l.createElement("button",{title:"Edit comment",className:"icon-button",onClick:i(()=>tt(!0),"onClick")},zl):null,D?l.createElement("button",{title:"Delete comment",className:"icon-button",onClick:i(()=>Be({id:C,pullRequestReviewId:K}),"onClick")},cn):null),l.createElement(Kl,{comment:d,bodyHTML:Ie,body:Y,canApplyPatch:He.isCurrentlyCheckedOut,allowEmpty:!!o.allowEmpty,specialDisplayBodyPostfix:d.specialDisplayBodyPostfix}),u)}i(zn,"CommentView");function Vn(o){return o.authorAssociation!==void 0}i(Vn,"isReviewEvent");function uo(o){return o&&typeof o=="object"&&typeof o.body=="string"&&typeof o.diffHunk=="string"}i(uo,"isIComment");const Ql={PENDING:"will review",COMMENTED:"reviewed",CHANGES_REQUESTED:"requested changes",APPROVED:"approved"},co=i(o=>Ql[o]||"reviewed","reviewDescriptor");function fo({for:o,onFocus:a,onMouseEnter:u,onMouseLeave:d,children:f}){var p,g;const C="htmlUrl"in o?o.htmlUrl:o.url,k=(g=uo(o)&&o.isDraft)!=null?g:Vn(o)&&((p=o.state)==null?void 0:p.toLocaleUpperCase())==="PENDING",D="user"in o?o.user:o.author,K="createdAt"in o?o.createdAt:o.submittedAt;return l.createElement("div",{className:"comment-container comment review-comment",onFocus:a,onMouseEnter:u,onMouseLeave:d},l.createElement("div",{className:"review-comment-container"},l.createElement("h3",{className:`review-comment-header${Vn(o)&&o.comments.length>0?"":" no-details"}`},l.createElement(Gr,null,l.createElement(yt,{for:D}),l.createElement(Xe,{for:D}),Vn(o)?Li(o):null,K?l.createElement(l.Fragment,null,Vn(o)&&o.state?co(o.state):"commented",Ue,l.createElement(Et,{href:C,date:K})):l.createElement("em",null,"pending"),k?l.createElement(l.Fragment,null,l.createElement("span",{className:"pending-label"},"Pending")):null)),f))}i(fo,"CommentBox");function mo({id:o,body:a,onCancel:u,onSave:d}){const{updateDraft:f}=(0,l.useContext)(B),p=(0,l.useRef)({body:a,dirty:!1}),g=(0,l.useRef)();(0,l.useEffect)(()=>{const Y=setInterval(()=>{p.current.dirty&&(f(o,p.current.body),p.current.dirty=!1)},500);return()=>clearInterval(Y)},[p]);const C=(0,l.useCallback)(async()=>{const{markdown:Y,submitButton:ne}=g.current;ne.disabled=!0;try{await d(Y.value)}finally{ne.disabled=!1}},[g,d]),k=(0,l.useCallback)(Y=>{Y.preventDefault(),C()},[C]),D=(0,l.useCallback)(Y=>{(Y.metaKey||Y.ctrlKey)&&Y.key==="Enter"&&(Y.preventDefault(),C())},[C]),K=(0,l.useCallback)(Y=>{p.current.body=Y.target.value,p.current.dirty=!0},[p]);return l.createElement("form",{ref:g,onSubmit:k},l.createElement("textarea",{name:"markdown",defaultValue:a,onKeyDown:D,onInput:K}),l.createElement("div",{className:"form-actions"},l.createElement("button",{className:"secondary",onClick:u},"Cancel"),l.createElement("button",{type:"submit",name:"submitButton"},"Save")))}i(mo,"EditComment");const Kl=i(({comment:o,bodyHTML:a,body:u,canApplyPatch:d,allowEmpty:f,specialDisplayBodyPostfix:p})=>{var g,C;if(!u&&!a)return f?null:l.createElement("div",{className:"comment-body"},l.createElement("em",null,"No description provided."));const{applyPatch:k}=(0,l.useContext)(B),D=l.createElement("div",{dangerouslySetInnerHTML:{__html:a!=null?a:""}}),Y=((C=(g=u||a)==null?void 0:g.indexOf("```diff"))!=null?C:-1)>-1&&d&&o?l.createElement("button",{onClick:i(()=>k(o),"onClick")},"Apply Patch"):l.createElement(l.Fragment,null);return l.createElement("div",{className:"comment-body"},D,Y,p?l.createElement("br",null):null,p?l.createElement("em",null,p):null,l.createElement(Yl,{reactions:o==null?void 0:o.reactions}))},"CommentBody"),Yl=i(({reactions:o})=>{if(!Array.isArray(o)||o.length===0)return null;const a=o.filter(u=>u.count>0);return a.length===0?null:l.createElement("div",{className:"comment-reactions",style:{marginTop:6}},a.map((u,d)=>{const p=u.reactors||[],g=p.slice(0,10),C=p.length>10?p.length-10:0;let k="";return g.length>0&&(C>0?k=`${Sr(g)} and ${C} more reacted with ${u.label}`:k=`${Sr(g)} reacted with ${u.label}`),l.createElement("div",{key:u.label+d,title:k},l.createElement("span",{className:"reaction-label"},u.label),Ue,u.count>1?l.createElement("span",{className:"reaction-count"},u.count):null)}))},"CommentReactions");function Lr({pendingCommentText:o,state:a,hasWritePermission:u,isIssue:d,isAuthor:f,continueOnGitHub:p,currentUserReviewState:g,lastReviewType:C,busy:k}){const{updatePR:D,requestChanges:K,approve:Y,close:ne,openOnGitHub:Ie,submit:$e}=(0,l.useContext)(B),[Be,be]=(0,l.useState)(!1),Re=(0,l.useRef)(),He=(0,l.useRef)();Ee.addListener("quoteReply",ze=>{var ut,Zo;const Ct=ze.replace(/\n/g,`
> `);D({pendingCommentText:`> ${Ct} 

`}),(ut=He.current)==null||ut.scrollIntoView(),(Zo=He.current)==null||Zo.focus()});const et=i(ze=>{ze.preventDefault();const{value:ut}=He.current;ne(ut)},"closeButton");let at=C!=null?C:g==="APPROVED"?te.Approve:g==="CHANGES_REQUESTED"?te.RequestChanges:te.Comment;async function tt(ze){const{value:ut}=He.current;if(p&&ze!==te.Comment){await Ie();return}switch(be(!0),ze){case te.RequestChanges:await K(ut);break;case te.Approve:await Y(ut);break;default:await $e(ut)}be(!1)}i(tt,"submitAction");const Ne=(0,l.useCallback)(ze=>{(ze.metaKey||ze.ctrlKey)&&ze.key==="Enter"&&tt(at)},[$e]);async function Zt(){await tt(at)}i(Zt,"defaultSubmitAction");const qe=f?{[te.Comment]:"Comment"}:p?{[te.Comment]:"Comment",[te.Approve]:"Approve on github.com",[te.RequestChanges]:"Request changes on github.com"}:je(d);return l.createElement("form",{id:"comment-form",ref:Re,className:"comment-form main-comment-form",onSubmit:i(()=>{var ze,ut;return $e((ut=(ze=He.current)==null?void 0:ze.value)!=null?ut:"")},"onSubmit")},l.createElement("textarea",{id:"comment-textarea",name:"body",ref:He,onInput:i(({target:ze})=>D({pendingCommentText:ze.value}),"onInput"),onKeyDown:Ne,value:o,placeholder:"Leave a comment"}),l.createElement("div",{className:"form-actions"},u||f?l.createElement("button",{id:"close",className:"secondary",disabled:Be||a!==de.Open,onClick:et,"data-command":"close"},d?"Close Issue":"Close Pull Request"):null,l.createElement(Yr,{optionsContext:i(()=>Si(qe,o),"optionsContext"),defaultAction:Zt,defaultOptionLabel:i(()=>qe[at],"defaultOptionLabel"),defaultOptionValue:i(()=>at,"defaultOptionValue"),allOptions:i(()=>{const ze=[];return qe.approve&&ze.push({label:qe[te.Approve],value:te.Approve,action:i(()=>tt(te.Approve),"action")}),qe.comment&&ze.push({label:qe[te.Comment],value:te.Comment,action:i(()=>tt(te.Comment),"action")}),qe.requestChanges&&ze.push({label:qe[te.RequestChanges],value:te.RequestChanges,action:i(()=>tt(te.RequestChanges),"action")}),ze},"allOptions"),optionsTitle:"Submit pull request review",disabled:Be||k,hasSingleAction:Object.keys(qe).length===1})))}i(Lr,"AddComment");function je(o){return o?vn:po}i(je,"commentMethods");const vn={comment:"Comment"},po={...vn,approve:"Approve",requestChanges:"Request Changes"},Si=i((o,a)=>{const u={preventDefaultContextMenuItems:!0,"github:reviewCommentMenu":!0};return o.approve&&(o.approve===po.approve?u["github:reviewCommentApprove"]=!0:u["github:reviewCommentApproveOnDotCom"]=!0),o.comment&&(u["github:reviewCommentComment"]=!0),o.requestChanges&&(o.requestChanges===po.requestChanges?u["github:reviewCommentRequestChanges"]=!0:u["github:reviewCommentRequestChangesOnDotCom"]=!0),u.body=a!=null?a:"",JSON.stringify(u)},"makeCommentMenuContext"),ho=i(o=>{var a,u;const{updatePR:d,requestChanges:f,approve:p,submit:g,openOnGitHub:C}=useContext(PullRequestContext),[k,D]=useState(!1),K=useRef();let Y=(a=o.lastReviewType)!=null?a:o.currentUserReviewState==="APPROVED"?ReviewType.Approve:o.currentUserReviewState==="CHANGES_REQUESTED"?ReviewType.RequestChanges:ReviewType.Comment;async function ne(Re){const{value:He}=K.current;if(o.continueOnGitHub&&Re!==ReviewType.Comment){await C();return}switch(D(!0),Re){case ReviewType.RequestChanges:await f(He);break;case ReviewType.Approve:await p(He);break;default:await g(He)}D(!1)}i(ne,"submitAction");async function Ie(){await ne(Y)}i(Ie,"defaultSubmitAction");const $e=i(Re=>{d({pendingCommentText:Re.target.value})},"onChangeTextarea"),Be=useCallback(Re=>{(Re.metaKey||Re.ctrlKey)&&Re.key==="Enter"&&(Re.preventDefault(),Ie())},[ne]),be=o.isAuthor?{comment:"Comment"}:o.continueOnGitHub?{comment:"Comment",approve:"Approve on github.com",requestChanges:"Request changes on github.com"}:je(o.isIssue);return React.createElement("span",{className:"comment-form"},React.createElement("textarea",{id:"comment-textarea",name:"body",placeholder:"Leave a comment",ref:K,value:(u=o.pendingCommentText)!=null?u:"",onChange:$e,onKeyDown:Be,disabled:k||o.busy}),React.createElement("div",{className:"comment-button"},React.createElement(ContextDropdown,{optionsContext:i(()=>Si(be,o.pendingCommentText),"optionsContext"),defaultAction:Ie,defaultOptionLabel:i(()=>be[Y],"defaultOptionLabel"),defaultOptionValue:i(()=>Y,"defaultOptionValue"),allOptions:i(()=>{const Re=[];return be.approve&&Re.push({label:be[ReviewType.Approve],value:ReviewType.Approve,action:i(()=>ne(ReviewType.Approve),"action")}),be.comment&&Re.push({label:be[ReviewType.Comment],value:ReviewType.Comment,action:i(()=>ne(ReviewType.Comment),"action")}),be.requestChanges&&Re.push({label:be[ReviewType.RequestChanges],value:ReviewType.RequestChanges,action:i(()=>ne(ReviewType.RequestChanges),"action")}),Re},"allOptions"),optionsTitle:"Submit pull request review",disabled:k||o.busy,hasSingleAction:Object.keys(be).length===1})))},"AddCommentSimple");function Sr(o){return o.length===0?"":o.length===1?o[0]:o.length===2?`${o[0]} and ${o[1]}`:`${o.slice(0,-1).join(", ")} and ${o[o.length-1]}`}i(Sr,"joinWithAnd");const $n=["copilot-pull-request-reviewer","copilot-swe-agent","Copilot"];var Ti=(o=>(o[o.None=0]="None",o[o.Started=1]="Started",o[o.Completed=2]="Completed",o[o.Failed=3]="Failed",o))(Ti||{});function vo(o){if(!o)return 0;switch(o.event){case G.CopilotStarted:return 1;case G.CopilotFinished:return 2;case G.CopilotFinishedError:return 3;default:return 0}}i(vo,"copilotEventToStatus");function Ni(o){for(let a=o.length-1;a>=0;a--)if(vo(o[a])!==0)return o[a]}i(Ni,"mostRecentCopilotEvent");function Gl({canEdit:o,state:a,head:u,base:d,title:f,titleHTML:p,number:g,url:C,author:k,isCurrentlyCheckedOut:D,isDraft:K,isIssue:Y,repositoryDefaultBranch:ne,events:Ie}){const[$e,Be]=we(f),[be,Re]=(0,l.useState)(!1);return l.createElement(l.Fragment,null,l.createElement(go,{title:$e,titleHTML:p,number:g,url:C,inEditMode:be,setEditMode:Re,setCurrentTitle:Be}),l.createElement(It,{state:a,head:u,base:d,author:k,isIssue:Y,isDraft:K}),l.createElement("div",{className:"header-actions"},l.createElement(yo,{isCurrentlyCheckedOut:D,isIssue:Y,canEdit:o,repositoryDefaultBranch:ne,setEditMode:Re}),l.createElement(Mi,{canEdit:o,codingAgentEvent:Ni(Ie)})))}i(Gl,"Header");function go({title:o,titleHTML:a,number:u,url:d,inEditMode:f,setEditMode:p,setCurrentTitle:g}){const{setTitle:C}=(0,l.useContext)(B);return f?l.createElement("form",{className:"editing-form title-editing-form",onSubmit:i(async Y=>{Y.preventDefault();try{const ne=Y.target[0].value;await C(ne),g(ne)}finally{p(!1)}},"onSubmit")},l.createElement("input",{type:"text",style:{width:"100%"},defaultValue:o}),l.createElement("div",{className:"form-actions"},l.createElement("button",{type:"button",className:"secondary",onClick:i(()=>p(!1),"onClick")},"Cancel"),l.createElement("button",{type:"submit"},"Update"))):l.createElement("div",{className:"overview-title"},l.createElement("h2",null,l.createElement("span",{dangerouslySetInnerHTML:{__html:a}})," ",l.createElement("a",{href:d,title:d},"#",u)))}i(go,"Title");function yo({isCurrentlyCheckedOut:o,canEdit:a,isIssue:u,repositoryDefaultBranch:d,setEditMode:f}){const{refresh:p,copyPrLink:g,copyVscodeDevLink:C,openChanges:k}=(0,l.useContext)(B),D=i(K=>{const Y=K.ctrlKey||K.metaKey;k(Y)},"handleOpenChangesClick");return l.createElement("div",{className:"button-group"},l.createElement(Tr,{isCurrentlyCheckedOut:o,isIssue:u,repositoryDefaultBranch:d}),!u&&l.createElement("button",{title:"Open Changes (Ctrl/Cmd+Click to open in second editor group)",onClick:D,className:"small-button"},"Open Changes"),l.createElement("button",{title:"Refresh with the latest data from GitHub",onClick:p,className:"secondary small-button"},"Refresh"),a&&l.createElement(l.Fragment,null,l.createElement("button",{title:"Rename",onClick:f,className:"secondary small-button"},"Rename"),l.createElement("button",{title:"Copy GitHub pull request link",onClick:g,className:"secondary small-button"},"Copy Link"),l.createElement("button",{title:"Copy vscode.dev link for viewing this pull request in VS Code for the Web",onClick:C,className:"secondary small-button"},"Copy vscode.dev Link")))}i(yo,"ButtonGroup");function Mi({canEdit:o,codingAgentEvent:a}){const{cancelCodingAgent:u,updatePR:d,openSessionLog:f}=(0,l.useContext)(B),[p,g]=(0,l.useState)(!1),C=i(async()=>{if(!a)return;g(!0);const D=await u(a);D.events.length>0&&d(D),g(!1)},"cancel"),k=a==null?void 0:a.sessionLink;return o&&a&&vo(a)===Ti.Started?l.createElement("div",{className:"button-group"},k&&l.createElement("button",{title:"View Session",className:"secondary small-button",onClick:i(()=>f(k),"onClick")},"View Session"),l.createElement("button",{title:"Cancel Coding Agent",disabled:p,className:"small-button",onClick:C},"Cancel Coding Agent")):null}i(Mi,"CancelCodingAgentButton");function It({state:o,isDraft:a,isIssue:u,author:d,base:f,head:p}){const{text:g,color:C,icon:k}=Co(o,a,u);return l.createElement("div",{className:"subtitle"},l.createElement("div",{id:"status",className:`status-badge-${C}`},l.createElement("span",{className:"icon"},k),l.createElement("span",null,g)),l.createElement("div",{className:"author"},l.createElement(yt,{for:d}),l.createElement("div",{className:"merge-branches"},l.createElement(Xe,{for:d})," ",u?null:l.createElement(l.Fragment,null,Ri(o)," into"," ",l.createElement("code",{className:"branch-tag"},f)," from ",l.createElement("code",{className:"branch-tag"},p)))))}i(It,"Subtitle");const Tr=i(({isCurrentlyCheckedOut:o,isIssue:a,repositoryDefaultBranch:u})=>{const{exitReviewMode:d,checkout:f}=(0,l.useContext)(B),[p,g]=(0,l.useState)(!1),C=i(async k=>{try{switch(g(!0),k){case"checkout":await f();break;case"exitReviewMode":await d();break;default:throw new Error(`Can't find action ${k}`)}}finally{g(!1)}},"onClick");return o?l.createElement(l.Fragment,null,l.createElement("button",{"aria-live":"polite",title:"Switch to a different branch than this pull request branch",disabled:p,className:"small-button",onClick:i(()=>C("exitReviewMode"),"onClick")},Se,Ue," Checkout '",u,"'")):a?null:l.createElement("button",{"aria-live":"polite",title:"Checkout a local copy of this pull request branch to verify or edit changes",disabled:p,className:"small-button",onClick:i(()=>C("checkout"),"onClick")},"Checkout")},"CheckoutButtons");function Co(o,a,u){const d=u?di:fr,f=u?Zr:si;return o===de.Merged?{text:"Merged",color:"merged",icon:Pt}:o===de.Open?a?{text:"Draft",color:"draft",icon:ai}:{text:"Open",color:"open",icon:f}:{text:"Closed",color:"closed",icon:d}}i(Co,"getStatus");function Ri(o){return o===de.Merged?"merged changes":"wants to merge changes"}i(Ri,"getActionText");function wo(o){const{reviewer:a,state:u}=o.reviewState,{reRequestReview:d}=(0,l.useContext)(B),f=o.event?O(o.event):void 0;return l.createElement("div",{className:"section-item reviewer"},l.createElement("div",{className:"avatar-with-author"},l.createElement(yt,{for:a}),l.createElement(Xe,{for:a})),l.createElement("div",{className:"reviewer-icons"},u!=="REQUESTED"&&(ot(a)||a.accountType!==Ze.Bot)?l.createElement("button",{className:"icon-button",title:"Re-request review",onClick:i(()=>d(o.reviewState.reviewer.id),"onClick")},ui,"\uFE0F"):null,Pi[u],f?l.createElement("div",{role:"alert","aria-label":f}):null))}i(wo,"Reviewer");const Pi={REQUESTED:(0,l.cloneElement)(dn,{className:"section-icon requested",title:"Awaiting requested review"}),COMMENTED:(0,l.cloneElement)(Br,{className:"section-icon commented",Root:"div",title:"Left review comments"}),APPROVED:(0,l.cloneElement)(Se,{className:"section-icon approved",title:"Approved these changes"}),CHANGES_REQUESTED:(0,l.cloneElement)(mr,{className:"section-icon changes",title:"Requested changes"})},Oi=i(({busy:o,baseHasMergeQueue:a})=>o?l.createElement("label",{htmlFor:"automerge-checkbox",className:"automerge-checkbox-label"},"Setting..."):l.createElement("label",{htmlFor:"automerge-checkbox",className:"automerge-checkbox-label"},a?"Merge when ready":"Auto-merge"),"AutoMergeLabel"),xo=i(({updateState:o,baseHasMergeQueue:a,allowAutoMerge:u,defaultMergeMethod:d,mergeMethodsAvailability:f,autoMerge:p,isDraft:g})=>{if(!u&&!p||!f||!d)return null;const C=l.useRef(),[k,D]=l.useState(!1),K=i(()=>{var Y,ne;return(ne=(Y=C.current)==null?void 0:Y.value)!=null?ne:"merge"},"selectedMethod");return l.createElement("div",{className:"automerge-section"},l.createElement("div",{className:"automerge-checkbox-wrapper"},l.createElement("input",{id:"automerge-checkbox",type:"checkbox",name:"automerge",checked:p,disabled:!u||g||k,onChange:i(async()=>{D(!0),await o({autoMerge:!p,autoMergeMethod:K()}),D(!1)},"onChange")})),l.createElement(Oi,{busy:k,baseHasMergeQueue:a}),a?null:l.createElement("div",{className:"merge-select-container"},l.createElement(Ro,{ref:C,defaultMergeMethod:d,mergeMethodsAvailability:f,onChange:i(async()=>{D(!0),await o({autoMergeMethod:K()}),D(!1)},"onChange"),disabled:k})))},"AutoMerge"),Di=i(({mergeQueueEntry:o})=>{const a=l.useContext(B);let u,d;switch(o.state){case Te.Mergeable:case Te.AwaitingChecks:case Te.Queued:{d=l.createElement("span",{className:"merge-queue-pending"},"Queued to merge..."),o.position===1?u=l.createElement("span",null,"This pull request is at the head of the ",l.createElement("a",{href:o.url},"merge queue"),"."):u=l.createElement("span",null,"This pull request is in the ",l.createElement("a",{href:o.url},"merge queue"),".");break}case Te.Locked:{d=l.createElement("span",{className:"merge-queue-blocked"},"Merging is blocked"),u=l.createElement("span",null,"The base branch does not allow updates");break}case Te.Unmergeable:{d=l.createElement("span",{className:"merge-queue-blocked"},"Merging is blocked"),u=l.createElement("span",null,"There are conflicts with the base branch.");break}}return l.createElement("div",{className:"merge-queue-container"},l.createElement("div",{className:"merge-queue"},l.createElement("div",{className:"merge-queue-icon"}),l.createElement("div",{className:"merge-queue-title"},d),u),l.createElement("div",{className:"button-container"},l.createElement("button",{onClick:a.dequeue},"Remove from Queue")))},"QueuedToMerge");var jn,Eo=new Uint8Array(16);function ko(){if(!jn&&(jn=typeof crypto!="undefined"&&crypto.getRandomValues&&crypto.getRandomValues.bind(crypto)||typeof msCrypto!="undefined"&&typeof msCrypto.getRandomValues=="function"&&msCrypto.getRandomValues.bind(msCrypto),!jn))throw new Error("crypto.getRandomValues() not supported. See https://github.com/uuidjs/uuid#getrandomvalues-not-supported");return jn(Eo)}i(ko,"rng");const bo=/^(?:[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}|00000000-0000-0000-0000-000000000000)$/i;function _o(o){return typeof o=="string"&&bo.test(o)}i(_o,"validate");const Ai=_o;for(var Je=[],Bn=0;Bn<256;++Bn)Je.push((Bn+256).toString(16).substr(1));function Xl(o){var a=arguments.length>1&&arguments[1]!==void 0?arguments[1]:0,u=(Je[o[a+0]]+Je[o[a+1]]+Je[o[a+2]]+Je[o[a+3]]+"-"+Je[o[a+4]]+Je[o[a+5]]+"-"+Je[o[a+6]]+Je[o[a+7]]+"-"+Je[o[a+8]]+Je[o[a+9]]+"-"+Je[o[a+10]]+Je[o[a+11]]+Je[o[a+12]]+Je[o[a+13]]+Je[o[a+14]]+Je[o[a+15]]).toLowerCase();if(!Ai(u))throw TypeError("Stringified UUID is invalid");return u}i(Xl,"stringify");const gn=Xl;function Ii(o,a,u){o=o||{};var d=o.random||(o.rng||ko)();if(d[6]=d[6]&15|64,d[8]=d[8]&63|128,a){u=u||0;for(var f=0;f<16;++f)a[u+f]=d[f];return a}return gn(d)}i(Ii,"v4");const Lo=Ii;var Ut=(o=>(o[o.esc=27]="esc",o[o.down=40]="down",o[o.up=38]="up",o))(Ut||{});const Nr=i(({options:o,defaultOption:a,disabled:u,submitAction:d,changeAction:f})=>{const[p,g]=(0,l.useState)(a),[C,k]=(0,l.useState)(!1),D=Lo(),K=`expandOptions${D}`,Y=i(()=>{k(!C)},"onClick"),ne=i(Be=>{g(Be.target.value),k(!1);const be=document.getElementById(`confirm-button${D}`);be==null||be.focus(),f&&f(Be.target.value)},"onMethodChange"),Ie=i(Be=>{if(C){const be=document.activeElement;switch(Be.keyCode){case 27:k(!1);const Re=document.getElementById(K);Re==null||Re.focus();break;case 40:if(!(be!=null&&be.id)||be.id===K){const He=document.getElementById(`${D}option0`);He==null||He.focus()}else{const He=new RegExp(`${D}option([0-9])`),et=be.id.match(He);if(et!=null&&et.length){const at=parseInt(et[1]);if(at<Object.entries(o).length-1){const tt=document.getElementById(`${D}option${at+1}`);tt==null||tt.focus()}}}break;case 38:if(!(be!=null&&be.id)||be.id===K){const He=Object.entries(o).length-1,et=document.getElementById(`${D}option${He}`);et==null||et.focus()}else{const He=new RegExp(`${D}option([0-9])`),et=be.id.match(He);if(et!=null&&et.length){const at=parseInt(et[1]);if(at>0){const tt=document.getElementById(`${D}option${at-1}`);tt==null||tt.focus()}}}break}}},"onKeyDown"),$e=Object.entries(o).length===1?"hidden":C?"open":"";return l.createElement("div",{className:"select-container",onKeyDown:Ie},l.createElement("div",{className:"select-control"},l.createElement(Un,{dropdownId:D,className:Object.keys(o).length>1?"select-left":"",options:o,selected:p,submitAction:d,disabled:!!u}),l.createElement("div",{className:"split"}),l.createElement("button",{id:K,className:"select-right "+$e,"aria-label":"Expand button options",onClick:Y},jr)),l.createElement("div",{className:C?"options-select":"hidden"},Object.entries(o).map(([Be,be],Re)=>l.createElement("button",{id:`${D}option${Re}`,key:Be,value:Be,onClick:ne},be))))},"Dropdown");function Un({dropdownId:o,className:a,options:u,selected:d,disabled:f,submitAction:p}){const[g,C]=(0,l.useState)(!1),k=i(async D=>{D.preventDefault();try{C(!0),await p(d)}finally{C(!1)}},"onSubmit");return l.createElement("form",{onSubmit:k},l.createElement("input",{disabled:g||f,type:"submit",className:a,id:`confirm-button${o}`,value:u[d]}))}i(Un,"Confirm");const Wn=i(({pr:o,isSimple:a})=>o.state===de.Merged?l.createElement("div",{className:"branch-status-message"},l.createElement("div",{className:"branch-status-icon"},a?Pt:null)," ","Pull request successfully merged."):o.state===de.Closed?l.createElement("div",{className:"branch-status-message"},"This pull request is closed."):null,"PRStatusMessage"),qn=i(({pr:o})=>o.state===de.Open?null:l.createElement(Mr,{...o}),"DeleteOption"),rn=i(({pr:o})=>{var a;const{state:u,status:d}=o,[f,p]=(0,l.useReducer)(g=>!g,(a=d==null?void 0:d.statuses.some(g=>g.state===U.Failure))!=null?a:!1);return(0,l.useEffect)(()=>{var g;(g=d==null?void 0:d.statuses.some(C=>C.state===U.Failure))!=null&&g?f||p():f&&p()},d==null?void 0:d.statuses),u===de.Open&&(d!=null&&d.statuses.length)?l.createElement(l.Fragment,null,l.createElement("div",{className:"status-section"},l.createElement("div",{className:"status-item"},l.createElement(Vi,{state:d.state}),l.createElement("p",{className:"status-item-detail-text"},ns(d.statuses)),l.createElement("button",{id:"status-checks-display-button",className:"secondary small-button",onClick:p,"aria-expanded":f},f?"Hide":"Show")),f?l.createElement(ts,{statuses:d.statuses}):null)):null},"StatusChecks"),So=i(({pr:o})=>{const{state:a,reviewRequirement:u}=o;return!u||a!==de.Open?null:l.createElement(l.Fragment,null,l.createElement("div",{className:"status-section"},l.createElement("div",{className:"status-item"},l.createElement(Po,{state:u.state}),l.createElement("p",{className:"status-item-detail-text"},Zn(u)))))},"RequiredReviewers"),Ht=i(({pr:o,isSimple:a})=>{if(!a||o.state!==de.Open||o.reviewers.length===0)return null;const u=[],d=new Set(o.reviewers);let f=o.events.length-1;for(;f>=0&&d.size>0;){const p=o.events[f];if(p.event===G.Reviewed){for(const g of d)if(p.user.id===g.reviewer.id){u.push({event:p,reviewState:g}),d.delete(g);break}}f--}return l.createElement("div",{className:"section"}," ",u.map(p=>l.createElement(wo,{key:nt(p.reviewState.reviewer),...p})))},"InlineReviewers"),Hi=i(({pr:o,isSimple:a})=>o.isIssue?null:l.createElement("div",{id:"status-checks"},l.createElement(l.Fragment,null,l.createElement(Wn,{pr:o,isSimple:a}),l.createElement(So,{pr:o}),l.createElement(rn,{pr:o}),l.createElement(Ht,{pr:o,isSimple:a}),l.createElement(Fi,{pr:o,isSimple:a}),l.createElement(qn,{pr:o}))),"StatusChecksSection"),Fi=i(({pr:o,isSimple:a})=>{const{create:u,checkMergeability:d}=(0,l.useContext)(B);if(a&&o.state!==de.Open)return l.createElement("div",{className:"branch-status-container"},l.createElement("form",null,l.createElement("button",{type:"submit",onClick:u},"Create New Pull Request...")));if(o.state!==de.Open)return null;const{mergeable:f}=o,[p,g]=(0,l.useState)(f);return f!==p&&f!==Ce.Unknown&&g(f),(0,l.useEffect)(()=>{const C=setInterval(async()=>{if(p===Ce.Unknown){const k=await d();g(k)}},3e3);return()=>clearInterval(C)},[p]),l.createElement("div",null,l.createElement(To,{mergeable:p,isSimple:a,isCurrentlyCheckedOut:o.isCurrentlyCheckedOut,canUpdateBranch:o.canUpdateBranch}),l.createElement(Jl,{mergeable:p,isSimple:a,isCurrentlyCheckedOut:o.isCurrentlyCheckedOut,canUpdateBranch:o.canUpdateBranch}),l.createElement(No,{pr:{...o,mergeable:p},isSimple:a}))},"MergeStatusAndActions"),Ca=null,To=i(({mergeable:o,isSimple:a,isCurrentlyCheckedOut:u,canUpdateBranch:d})=>{const{updateBranch:f}=(0,l.useContext)(B),[p,g]=(0,l.useState)(!1),C=i(()=>{g(!0),f().finally(()=>g(!1))},"onClick");let k=dn,D="Checking if this branch can be merged...",K=null;return o===Ce.Mergeable?(k=Se,D="This branch has no conflicts with the base branch."):o===Ce.Conflict?(k=Ot,D="This branch has conflicts that must be resolved.",K="Resolve conflicts"):o===Ce.NotMergeable?(k=Ot,D="Branch protection policy must be fulfilled before merging."):o===Ce.Behind&&(k=Ot,D="This branch is out-of-date with the base branch.",K="Update with merge commit"),a&&(k=null,o!==Ce.Conflict&&(K=null)),l.createElement("div",{className:"status-item status-section"},k,l.createElement("p",null,D),K&&d?l.createElement("div",{className:"button-container"},l.createElement("button",{className:"secondary",onClick:C,disabled:p},K)):null)},"MergeStatus"),Jl=i(({mergeable:o,isSimple:a,isCurrentlyCheckedOut:u,canUpdateBranch:d})=>{const{updateBranch:f}=(0,l.useContext)(B),[p,g]=(0,l.useState)(!1),C=i(()=>{g(!0),f().finally(()=>g(!1))},"update"),k=!u&&o===Ce.Conflict;return!d||k||a||o===Ce.Behind||o===Ce.Conflict||o===Ce.Unknown?null:l.createElement("div",{className:"status-item status-section"},ke,l.createElement("p",null,"This branch is out-of-date with the base branch."),l.createElement("button",{className:"secondary",onClick:C,disabled:p},"Update with Merge Commit"))},"OfferToUpdate"),yn=i(({isSimple:o})=>{const[a,u]=(0,l.useState)(!1),{readyForReview:d,updatePR:f}=(0,l.useContext)(B),p=(0,l.useCallback)(async()=>{try{u(!0);const g=await d();f(g)}finally{u(!1)}},[u,d,f]);return l.createElement("div",{className:"ready-for-review-container"},l.createElement("div",{className:"ready-for-review-text-wrapper"},l.createElement("div",{className:"ready-for-review-icon"},o?null:ke),l.createElement("div",null,l.createElement("div",{className:"ready-for-review-heading"},"This pull request is still a work in progress."),l.createElement("div",{className:"ready-for-review-meta"},"Draft pull requests cannot be merged."))),l.createElement("div",{className:"button-container"},l.createElement("button",{disabled:a,onClick:p},"Ready for Review")))},"ReadyForReview"),Wt=i(o=>{const a=(0,l.useContext)(B),u=(0,l.useRef)(),[d,f]=(0,l.useState)(null);return o.mergeQueueMethod?l.createElement("div",null,l.createElement("div",{id:"merge-comment-form"},l.createElement("button",{onClick:i(()=>a.enqueue(),"onClick")},"Add to Merge Queue"))):d?l.createElement(Rr,{pr:o,method:d,cancel:i(()=>f(null),"cancel")}):l.createElement("div",{className:"automerge-section wrapper"},l.createElement("button",{onClick:i(()=>f(u.current.value),"onClick")},"Merge Pull Request"),Ue,"using method",Ue,l.createElement(Ro,{ref:u,...o}))},"Merge"),No=i(({pr:o,isSimple:a})=>{var u;const{hasWritePermission:d,canEdit:f,isDraft:p,mergeable:g}=o;if(p)return f?l.createElement(yn,{isSimple:a}):null;if(g===Ce.Mergeable&&d&&!o.mergeQueueEntry)return a?l.createElement(zi,{...o}):l.createElement(Wt,{...o});if(!a&&d&&!o.mergeQueueEntry){const C=(0,l.useContext)(B);return l.createElement(xo,{updateState:i(k=>C.updateAutoMerge(k),"updateState"),...o,baseHasMergeQueue:!!o.mergeQueueMethod,defaultMergeMethod:(u=o.autoMergeMethod)!=null?u:o.defaultMergeMethod})}else if(o.mergeQueueEntry)return l.createElement(Di,{mergeQueueEntry:o.mergeQueueEntry});return null},"PrActions"),Mo=i(()=>{const{openOnGitHub:o}=useContext(PullRequestContext);return React.createElement("button",{id:"merge-on-github",type:"submit",onClick:i(()=>o(),"onClick")},"Merge on github.com")},"MergeOnGitHub"),zi=i(o=>{const{merge:a,updatePR:u}=(0,l.useContext)(B);async function d(p){const g=await a({title:"",description:"",method:p});u(g)}i(d,"submitAction");const f=Object.keys(Pr).filter(p=>o.mergeMethodsAvailability[p]).reduce((p,g)=>(p[g]=Pr[g],p),{});return l.createElement(Nr,{options:f,defaultOption:o.defaultMergeMethod,submitAction:d})},"MergeSimple"),Mr=i(o=>{const{deleteBranch:a}=(0,l.useContext)(B),[u,d]=(0,l.useState)(!1);return o.isRemoteHeadDeleted!==!1&&o.isLocalHeadDeleted!==!1?l.createElement("div",null):l.createElement("div",{className:"branch-status-container"},l.createElement("form",{onSubmit:i(async f=>{f.preventDefault();try{d(!0);const p=await a();p&&p.cancelled&&d(!1)}finally{d(!1)}},"onSubmit")},l.createElement("button",{disabled:u,className:"secondary",type:"submit"},"Delete Branch...")))},"DeleteBranch");function Rr({pr:o,method:a,cancel:u}){const{merge:d,updatePR:f,changeEmail:p}=(0,l.useContext)(B),[g,C]=(0,l.useState)(!1),k=o.emailForCommit;return l.createElement("div",null,l.createElement("form",{id:"merge-comment-form",onSubmit:i(async D=>{D.preventDefault();try{C(!0);const{title:K,description:Y}=D.target,ne=await d({title:K==null?void 0:K.value,description:Y==null?void 0:Y.value,method:a,email:k});f(ne)}finally{C(!1)}},"onSubmit")},a==="rebase"?null:l.createElement("input",{type:"text",name:"title",defaultValue:vt(a,o)}),a==="rebase"?null:l.createElement("textarea",{name:"description",defaultValue:es(a,o)}),a==="rebase"||!k?null:l.createElement("div",{className:"commit-association"},l.createElement("span",null,"Commit will be associated with ",l.createElement("button",{className:"input-box",title:"Change email","aria-label":"Change email",disabled:g,onClick:i(()=>{C(!0),p(k).finally(()=>C(!1))},"onClick")},k))),l.createElement("div",{className:"form-actions",id:a==="rebase"?"rebase-actions":""},l.createElement("button",{className:"secondary",onClick:u},"Cancel"),l.createElement("button",{disabled:g,type:"submit",id:"confirm-merge"},a==="rebase"?"Confirm ":"",Pr[a]))))}i(Rr,"ConfirmMerge");function vt(o,a){var u,d,f,p;switch(o){case"merge":return(d=(u=a.mergeCommitMeta)==null?void 0:u.title)!=null?d:`Merge pull request #${a.number} from ${a.head}`;case"squash":return(p=(f=a.squashCommitMeta)==null?void 0:f.title)!=null?p:`${a.title} (#${a.number})`;default:return""}}i(vt,"getDefaultTitleText");function es(o,a){var u,d,f,p;switch(o){case"merge":return(d=(u=a.mergeCommitMeta)==null?void 0:u.description)!=null?d:a.title;case"squash":return(p=(f=a.squashCommitMeta)==null?void 0:f.description)!=null?p:"";default:return""}}i(es,"getDefaultDescriptionText");const Pr={merge:"Create Merge Commit",squash:"Squash and Merge",rebase:"Rebase and Merge"},Ro=l.forwardRef(({defaultMergeMethod:o,mergeMethodsAvailability:a,onChange:u,ariaLabel:d,name:f,title:p,disabled:g},C)=>l.createElement("select",{ref:C,defaultValue:o,onChange:u,disabled:g,"aria-label":d!=null?d:"Select merge method",name:f,title:p},Object.entries(Pr).map(([k,D])=>l.createElement("option",{key:k,value:k,disabled:!a[k]},D,a[k]?null:" (not enabled)")))),ts=i(({statuses:o})=>l.createElement("div",{className:"status-scroll"},o.map(a=>l.createElement("div",{key:a.id,className:"status-check"},l.createElement("div",{className:"status-check-details"},l.createElement(Vi,{state:a.state}),l.createElement(yt,{for:{avatarUrl:a.avatarUrl,url:a.url}}),l.createElement("span",{className:"status-check-detail-text"},a.workflowName?`${a.workflowName} / `:null,a.context,a.event?` (${a.event})`:null," ",a.description?`\u2014 ${a.description}`:null)),l.createElement("div",null,a.isRequired?l.createElement("span",{className:"label"},"Required"):null,a.targetUrl?l.createElement("a",{href:a.targetUrl,title:a.targetUrl},"Details"):null)))),"StatusCheckDetails");function ns(o){const a=ki(o,d=>{switch(d.state){case U.Success:case U.Failure:case U.Neutral:return d.state;default:return U.Pending}}),u=[];for(const d of Object.keys(a)){const f=a[d].length;let p="";switch(d){case U.Success:p="successful";break;case U.Failure:p="failed";break;case U.Neutral:p="skipped";break;default:p="pending"}const g=f>1?`${f} ${p} checks`:`${f} ${p} check`;u.push(g)}return u.join(" and ")}i(ns,"getSummaryLabel");function Vi({state:o}){switch(o){case U.Neutral:return ht;case U.Success:return Se;case U.Failure:return Ot}return dn}i(Vi,"StateIcon");function Po({state:o}){switch(o){case U.Pending:return mr;case U.Failure:return Ot}return Se}i(Po,"RequiredReviewStateIcon");function Zn(o){const a=o.approvals.length,u=o.requestedChanges.length,d=o.count;switch(o.state){case U.Failure:return`At least ${d} approving review${d>1?"s":""} is required by reviewers with write access.`;case U.Pending:return`${u} review${u>1?"s":""} requesting changes by reviewers with write access.`}return`${a} approving review${a>1?"s":""} by reviewers with write access.`}i(Zn,"getRequiredReviewSummary");function rs(o){const{name:a,canDelete:u,color:d}=o,f=wr(d,o.isDarkTheme,!1);return l.createElement("div",{className:"section-item label",style:{backgroundColor:f.backgroundColor,color:f.textColor,borderColor:`${f.borderColor}`,paddingRight:u?"2px":"8px"}},a,o.children)}i(rs,"Label");function os(o){const{name:a,color:u}=o,d=gitHubLabelColor(u,o.isDarkTheme,!1);return React.createElement("li",{style:{backgroundColor:d.backgroundColor,color:d.textColor,borderColor:`${d.borderColor}`}},a,o.children)}i(os,"LabelCreate");function Oo({reviewers:o,labels:a,hasWritePermission:u,isIssue:d,projectItems:f,milestone:p,assignees:g,canAssignCopilot:C}){const{addReviewers:k,addAssignees:D,addAssigneeYourself:K,addAssigneeCopilot:Y,addLabels:ne,removeLabel:Ie,changeProjects:$e,addMilestone:Be,updatePR:be,pr:Re}=(0,l.useContext)(B),[He,et]=(0,l.useState)(!1),at=C&&g.every(Ne=>!$n.includes(Ne.login)),tt=i(async()=>{const Ne=await $e();be({...Ne})},"updateProjects");return l.createElement("div",{id:"sidebar"},d?"":l.createElement("div",{id:"reviewers",className:"section"},l.createElement("div",{className:"section-header",onClick:i(async()=>{const Ne=await k();be({reviewers:Ne.reviewers})},"onClick")},l.createElement("div",{className:"section-title"},"Reviewers"),u?l.createElement("button",{className:"icon-button",title:"Add Reviewers"},en):null),o&&o.length?o.map(Ne=>l.createElement(wo,{key:nt(Ne.reviewer),reviewState:Ne})):l.createElement("div",{className:"section-placeholder"},"None yet")),l.createElement("div",{id:"assignees",className:"section"},l.createElement("div",{className:"section-header",onClick:i(async Ne=>{if(Ne.target.closest("#assign-copilot-btn"))return;const qe=await D();be({assignees:qe.assignees,events:qe.events})},"onClick")},l.createElement("div",{className:"section-title"},"Assignees"),u?l.createElement("div",{className:"icon-button-group"},at?l.createElement("button",{id:"assign-copilot-btn",className:"icon-button",title:"Assign for Copilot to work on",disabled:He,onClick:i(async()=>{et(!0);try{const Ne=await Y();be({assignees:Ne.assignees,events:Ne.events})}finally{et(!1)}},"onClick")},fi):null,l.createElement("button",{className:"icon-button",title:"Add Assignees"},en)):null),g&&g.length?g.map((Ne,Zt)=>l.createElement("div",{key:Zt,className:"section-item reviewer"},l.createElement("div",{className:"avatar-with-author"},l.createElement(yt,{for:Ne}),l.createElement(Xe,{for:Ne})))):l.createElement("div",{className:"section-placeholder"},"None yet",Re.hasWritePermission?l.createElement(l.Fragment,null,"\u2014",l.createElement("a",{className:"assign-yourself",onClick:i(async()=>{const Ne=await K();be({assignees:Ne.assignees,events:Ne.events})},"onClick")},"assign yourself")):null)),l.createElement("div",{id:"labels",className:"section"},l.createElement("div",{className:"section-header",onClick:i(async()=>{const Ne=await ne();be({labels:Ne.added})},"onClick")},l.createElement("div",{className:"section-title"},"Labels"),u?l.createElement("button",{className:"icon-button",title:"Add Labels"},en):null),a.length?l.createElement("div",{className:"labels-list"},a.map(Ne=>l.createElement(rs,{key:Ne.name,...Ne,canDelete:u,isDarkTheme:Re.isDarkTheme},u?l.createElement("button",{className:"icon-button",onClick:i(()=>Ie(Ne.name),"onClick")},Ot,"\uFE0F"):null))):l.createElement("div",{className:"section-placeholder"},"None yet")),Re.isEnterprise?null:l.createElement("div",{id:"project",className:"section"},l.createElement("div",{className:"section-header",onClick:tt},l.createElement("div",{className:"section-title"},"Project"),u?l.createElement("button",{className:"icon-button",title:"Add Project"},en):null),f?f.length>0?f.map(Ne=>l.createElement($i,{key:Ne.project.title,...Ne,canDelete:u})):l.createElement("div",{className:"section-placeholder"},"None Yet"):l.createElement("a",{onClick:tt},"Sign in with more permissions to see projects")),l.createElement("div",{id:"milestone",className:"section"},l.createElement("div",{className:"section-header",onClick:i(async()=>{const Ne=await Be();be({milestone:Ne.added})},"onClick")},l.createElement("div",{className:"section-title"},"Milestone"),u?l.createElement("button",{className:"icon-button",title:"Add Milestone"},en):null),p?l.createElement(Ft,{key:p.title,...p,canDelete:u}):l.createElement("div",{className:"section-placeholder"},"No milestone")))}i(Oo,"Sidebar");function Ft(o){const{removeMilestone:a,updatePR:u,pr:d}=(0,l.useContext)(B),f=getComputedStyle(document.documentElement).getPropertyValue("--vscode-badge-foreground"),p=wr(f,d.isDarkTheme,!1),{canDelete:g,title:C}=o;return l.createElement("div",{className:"labels-list"},l.createElement("div",{className:"section-item label",style:{backgroundColor:p.backgroundColor,color:p.textColor,borderColor:`${p.borderColor}`}},C,g?l.createElement("button",{className:"icon-button",onClick:i(async()=>{await a(),u({milestone:void 0})},"onClick")},Ot,"\uFE0F"):null))}i(Ft,"Milestone");function $i(o){const{removeProject:a,updatePR:u,pr:d}=(0,l.useContext)(B),f=getComputedStyle(document.documentElement).getPropertyValue("--vscode-badge-foreground"),p=wr(f,d.isDarkTheme,!1),{canDelete:g}=o;return l.createElement("div",{className:"labels-list"},l.createElement("div",{className:"section-item label",style:{backgroundColor:p.backgroundColor,color:p.textColor,borderColor:`${p.borderColor}`}},o.project.title,g?l.createElement("button",{className:"icon-button",onClick:i(async()=>{var C;await a(o),u({projectItems:(C=d.projectItems)==null?void 0:C.filter(k=>k.id!==o.id)})},"onClick")},Ot,"\uFE0F"):null))}i($i,"Project");var ji=(o=>(o[o.ADD=0]="ADD",o[o.COPY=1]="COPY",o[o.DELETE=2]="DELETE",o[o.MODIFY=3]="MODIFY",o[o.RENAME=4]="RENAME",o[o.TYPE=5]="TYPE",o[o.UNKNOWN=6]="UNKNOWN",o[o.UNMERGED=7]="UNMERGED",o))(ji||{});const qt=class qt{constructor(a,u,d,f,p,g,C){this.baseCommit=a,this.status=u,this.fileName=d,this.previousFileName=f,this.patch=p,this.diffHunks=g,this.blobUrl=C}};i(qt,"file_InMemFileChange");let Do=qt;const Ve=class Ve{constructor(a,u,d,f,p){this.baseCommit=a,this.blobUrl=u,this.status=d,this.fileName=f,this.previousFileName=p}};i(Ve,"file_SlimFileChange");let on=Ve;var is=Object.defineProperty,ls=i((o,a,u)=>a in o?is(o,a,{enumerable:!0,configurable:!0,writable:!0,value:u}):o[a]=u,"diffHunk_defNormalProp"),ss=i((o,a,u)=>ls(o,typeof a!="symbol"?a+"":a,u),"diffHunk_publicField"),Bi=(o=>(o[o.Context=0]="Context",o[o.Add=1]="Add",o[o.Delete=2]="Delete",o[o.Control=3]="Control",o))(Bi||{});const We=class We{constructor(a,u,d,f,p,g=!0){this.type=a,this.oldLineNumber=u,this.newLineNumber=d,this.positionInHunk=f,this._raw=p,this.endwithLineBreak=g}get raw(){return this._raw}get text(){return this._raw.substr(1)}};i(We,"DiffLine");let Qn=We;function Ui(o){switch(o[0]){case" ":return 0;case"+":return 1;case"-":return 2;default:return 3}}i(Ui,"getDiffChangeType");const Tt=class Tt{constructor(a,u,d,f,p){this.oldLineNumber=a,this.oldLength=u,this.newLineNumber=d,this.newLength=f,this.positionInHunk=p,ss(this,"diffLines",[])}};i(Tt,"DiffHunk");let Or=Tt;const Cn=/^@@ \-(\d+)(,(\d+))?( \+(\d+)(,(\d+)?)?)? @@/;function Kn(o){let a=0,u=0;for(;(u=o.indexOf("\r",u))!==-1;)u++,a++;return a}i(Kn,"countCarriageReturns");function*as(o){let a=0;for(;a!==-1&&a<o.length;){const u=a;a=o.indexOf(`
`,a);let f=(a!==-1?a:o.length)-u;a!==-1&&(a>0&&o[a-1]==="\r"&&f--,a++),yield o.substr(u,f)}}i(as,"LineReader");function*Yn(o){const a=as(o);let u=a.next(),d,f=-1,p=-1,g=-1;for(;!u.done;){const C=u.value;if(Cn.test(C)){d&&(yield d,d=void 0),f===-1&&(f=0);const k=Cn.exec(C),D=p=Number(k[1]),K=Number(k[3])||1,Y=g=Number(k[5]),ne=Number(k[7])||1;d=new Or(D,K,Y,ne,f),d.diffLines.push(new Qn(3,-1,-1,f,C))}else if(d){const k=Ui(C);if(k===3)d.diffLines&&d.diffLines.length&&(d.diffLines[d.diffLines.length-1].endwithLineBreak=!1);else{d.diffLines.push(new Qn(k,k!==1?p:-1,k!==2?g:-1,f,C));const D=1+Kn(C);switch(k){case 0:p+=D,g+=D;break;case 2:p+=D;break;case 1:g+=D;break}}}f!==-1&&++f,u=a.next()}d&&(yield d)}i(Yn,"parseDiffHunk");function us(o){const a=Yn(o);let u=a.next();const d=[];for(;!u.done;){const f=u.value;d.push(f),u=a.next()}return d}i(us,"parsePatch");function Wi(o){const a=[],u=i(k=>({diffLines:[],newLength:0,oldLength:0,oldLineNumber:k.oldLineNumber,newLineNumber:k.newLineNumber,positionInHunk:0}),"newHunk");let d,f;const p=i((k,D)=>{k.diffLines.push(D),D.type===2?k.oldLength++:D.type===1?k.newLength++:D.type===0&&(k.oldLength++,k.newLength++)},"addLineToHunk"),g=i(k=>k.diffLines.some(D=>D.type!==0),"hunkHasChanges"),C=i(k=>g(k)&&k.diffLines[k.diffLines.length-1].type===0,"hunkHasSandwichedChanges");for(const k of o.diffLines)k.type===0?(d||(d=u(k)),p(d,k),C(d)&&(f||(f=u(k)),p(f,k))):(d||o.oldLineNumber===1&&(k.type===2||k.type===1))&&(d||(d=u(k)),C(d)&&(a.push(d),d=f,f=void 0),(k.type===2||k.type===1)&&p(d,k));return d&&a.push(d),a}i(Wi,"splitIntoSmallerHunks");function cs(o,a){const u=o.split(/\r?\n/),d=Yn(a);let f=d.next();const p=[],g=[];let C=0,k=!0;for(;!f.done;){const D=f.value;p.push(D);const K=D.oldLineNumber;for(let Y=C+1;Y<K;Y++)g.push(u[Y-1]);C=K+D.oldLength-1;for(let Y=0;Y<D.diffLines.length;Y++){const ne=D.diffLines[Y];if(!(ne.type===2||ne.type===3))if(ne.type===1)g.push(ne.text);else{const Ie=ne.text;g.push(Ie)}}if(f=d.next(),f.done){for(let Y=D.diffLines.length-1;Y>=0;Y--)if(D.diffLines[Y].type!==2){k=D.diffLines[Y].endwithLineBreak;break}}}if(k)if(C<u.length)for(let D=C+1;D<=u.length;D++)g.push(u[D-1]);else g.push("");return g.join(`
`)}i(cs,"getModifiedContentFromDiffHunk");function qi(o){switch(o){case"removed":return GitChangeType.DELETE;case"added":return GitChangeType.ADD;case"renamed":return GitChangeType.RENAME;case"modified":return GitChangeType.MODIFY;default:return GitChangeType.UNKNOWN}}i(qi,"getGitChangeType");async function wa(o,a){var u;const d=[];for(let f=0;f<o.length;f++){const p=o[f],g=qi(p.status);if(!p.patch&&g!==GitChangeType.RENAME&&g!==GitChangeType.MODIFY&&!(g===GitChangeType.ADD&&p.additions===0)){d.push(new SlimFileChange(a,p.blob_url,g,p.filename,p.previous_filename));continue}const C=p.patch?us(p.patch):void 0;d.push(new InMemFileChange(a,g,p.filename,p.previous_filename,(u=p.patch)!=null?u:"",C,p.blob_url))}return d}i(wa,"parseDiff");function ds({hunks:o}){return l.createElement("div",{className:"diff"},o.map((a,u)=>l.createElement(ms,{key:u,hunk:a})))}i(ds,"Diff");const fs=ds,ms=i(({hunk:o,maxLines:a=8})=>l.createElement(l.Fragment,null,o.diffLines.slice(-a).map(u=>l.createElement("div",{key:ps(u),className:`diffLine ${hs(u.type)}`},l.createElement(wn,{num:u.oldLineNumber}),l.createElement(wn,{num:u.newLineNumber}),l.createElement("div",{className:"diffTypeSign"},u._raw.substr(0,1)),l.createElement("div",{className:"lineContent"},u._raw.substr(1))))),"Hunk"),ps=i(o=>`${o.oldLineNumber}->${o.newLineNumber}`,"keyForDiffLine"),wn=i(({num:o})=>l.createElement("div",{className:"lineNumber"},o>0?o:" "),"LineNumber"),hs=i(o=>Bi[o].toLowerCase(),"getDiffChangeClass");function Zi(o){return o.event===G.Assigned||o.event===G.Unassigned}i(Zi,"isAssignUnassignEvent");const Ao=i(({events:o,isIssue:a})=>{var u,d,f,p;const g=[];for(let C=0;C<o.length;C++)if(C>0&&Zi(o[C])&&Zi(g[g.length-1])){const k=g[g.length-1],D=o[C];if(k.actor.login===D.actor.login&&new Date(k.createdAt).getTime()+1e3*60*10>new Date(D.createdAt).getTime()){const K=k.assignees||[],Y=k.unassignees||[],ne=(d=(u=D.assignees)==null?void 0:u.filter($e=>!K.some(Be=>Be.id===$e.id)))!=null?d:[],Ie=(p=(f=D.unassignees)==null?void 0:f.filter($e=>!Y.some(Be=>Be.id===$e.id)))!=null?p:[];k.assignees=[...K,...ne],k.unassignees=[...Y,...Ie]}else g.push(D)}else g.push(o[C]);return l.createElement(l.Fragment,null,g.map(C=>{switch(C.event){case G.Committed:return l.createElement(Qi,{key:`commit${C.id}`,...C});case G.Reviewed:return l.createElement(Gi,{key:`review${C.id}`,...C});case G.Commented:return l.createElement(ys,{key:`comment${C.id}`,...C});case G.Merged:return l.createElement(ln,{key:`merged${C.id}`,...C});case G.Assigned:return l.createElement(Io,{key:`assign${C.id}`,event:C});case G.Unassigned:return l.createElement(Io,{key:`unassign${C.id}`,event:C});case G.HeadRefDeleted:return l.createElement(Cs,{key:`head${C.id}`,...C});case G.CrossReferenced:return l.createElement(Jn,{key:`cross${C.id}`,...C});case G.Closed:return l.createElement(xn,{key:`closed${C.id}`,event:C,isIssue:a});case G.Reopened:return l.createElement(Ho,{key:`reopened${C.id}`,event:C,isIssue:a});case G.NewCommitsSinceReview:return l.createElement(Ki,{key:`newCommits${C.id}`});case G.CopilotStarted:return l.createElement(er,{key:`copilotStarted${C.id}`,...C});case G.CopilotFinished:return l.createElement(Fo,{key:`copilotFinished${C.id}`,...C});case G.CopilotFinishedError:return l.createElement(Ji,{key:`copilotFinishedError${C.id}`,...C});default:throw new gr(C)}}))},"Timeline"),vs=null,Qi=i(o=>l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},dt,Ue,l.createElement("div",{className:"avatar-container"},l.createElement(yt,{for:o.author})),l.createElement("div",{className:"message-container"},l.createElement("a",{className:"message",href:o.htmlUrl,title:o.htmlUrl},o.message.substr(0,o.message.indexOf(`
`)>-1?o.message.indexOf(`
`):o.message.length)))),l.createElement("div",{className:"timeline-detail"},l.createElement("a",{className:"sha",href:o.htmlUrl,title:o.htmlUrl},o.sha.slice(0,7)),l.createElement(Et,{date:o.committedDate}))),"CommitEventView"),Ki=i(()=>{const{gotoChangesSinceReview:o,pr:a}=(0,l.useContext)(B);if(!a.isCurrentlyCheckedOut)return null;const[u,d]=(0,l.useState)(!1),f=i(async()=>{d(!0),await o(),d(!1)},"viewChanges");return l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},Wr,Ue,l.createElement("span",{style:{fontWeight:"bold"}},"New changes since your last Review")),l.createElement("button",{"aria-live":"polite",title:"View the changes since your last review",onClick:f,disabled:u},"View Changes"))},"NewCommitsSinceReviewEventView"),Yi=i(o=>o.position!==null?`pos:${o.position}`:`ori:${o.originalPosition}`,"positionKey"),Gn=i(o=>ki(o,a=>a.path+":"+Yi(a)),"groupCommentsByPath"),Gi=i(o=>{const a=Gn(o.comments),u=o.state==="PENDING";return l.createElement(zn,{comment:o,allowEmpty:!0},o.comments.length?l.createElement("div",{className:"comment-body review-comment-body"},Object.entries(a).map(([d,f])=>l.createElement(Xn,{key:d,thread:f,event:o}))):null,u?l.createElement(gs,null):null)},"ReviewEventView");function Xn({thread:o,event:a}){var u;const d=o[0],[f,p]=(0,l.useState)(!d.isResolved),[g,C]=(0,l.useState)(!!d.isResolved),{openDiff:k,toggleResolveComment:D}=(0,l.useContext)(B),K=a.reviewThread&&(a.reviewThread.canResolve&&!a.reviewThread.isResolved||a.reviewThread.canUnresolve&&a.reviewThread.isResolved),Y=i(()=>{if(a.reviewThread){const ne=!g;p(!ne),C(ne),D(a.reviewThread.threadId,o,ne)}},"toggleResolve");return l.createElement("div",{key:a.id,className:"diff-container"},l.createElement("div",{className:"resolved-container"},l.createElement("div",null,d.position===null?l.createElement("span",null,l.createElement("span",null,d.path),l.createElement("span",{className:"outdatedLabel"},"Outdated")):l.createElement("a",{className:"diffPath",onClick:i(()=>k(d),"onClick")},d.path),!g&&!f?l.createElement("span",{className:"unresolvedLabel"},"Unresolved"):null),l.createElement("button",{className:"secondary",onClick:i(()=>p(!f),"onClick")},f?"Hide":"Show")),f?l.createElement("div",null,l.createElement(fs,{hunks:(u=d.diffHunks)!=null?u:[]}),o.map(ne=>l.createElement(zn,{key:ne.id,comment:ne})),K?l.createElement("div",{className:"resolve-comment-row"},l.createElement("button",{className:"secondary comment-resolve",onClick:i(()=>Y(),"onClick")},g?"Unresolve Conversation":"Resolve Conversation")):null):null)}i(Xn,"CommentThread");function gs(){const{requestChanges:o,approve:a,submit:u,pr:d}=(0,l.useContext)(B),{isAuthor:f}=d,p=(0,l.useRef)(),[g,C]=(0,l.useState)(!1);async function k(K,Y){K.preventDefault();const{value:ne}=p.current;switch(C(!0),Y){case te.RequestChanges:await o(ne);break;case te.Approve:await a(ne);break;default:await u(ne)}C(!1)}i(k,"submitAction");const D=i(K=>{(K.ctrlKey||K.metaKey)&&K.key==="Enter"&&k(K,te.Comment)},"onKeyDown");return l.createElement("form",null,l.createElement("textarea",{id:"pending-review",ref:p,placeholder:"Leave a review summary comment",onKeyDown:D}),l.createElement("div",{className:"form-actions"},f?null:l.createElement("button",{id:"request-changes",className:"secondary",disabled:g||d.busy,onClick:i(K=>k(K,te.RequestChanges),"onClick")},"Request Changes"),f?null:l.createElement("button",{id:"approve",className:"secondary",disabled:g||d.busy,onClick:i(K=>k(K,te.Approve),"onClick")},"Approve"),l.createElement("button",{disabled:g||d.busy,onClick:i(K=>k(K,te.Comment),"onClick")},"Submit Review")))}i(gs,"AddReviewSummaryComment");const ys=i(o=>l.createElement(zn,{headerInEditMode:!0,comment:o}),"CommentEventView"),ln=i(o=>{const{revert:a,pr:u}=(0,l.useContext)(B);return l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},Pt,Ue,l.createElement("div",{className:"avatar-container"},l.createElement(yt,{for:o.user})),l.createElement(Xe,{for:o.user}),l.createElement("div",{className:"message"},"merged commit",Ue,l.createElement("a",{className:"sha",href:o.commitUrl,title:o.commitUrl},o.sha.substr(0,7)),Ue,"into ",o.mergeRef,Ue)),u.revertable?l.createElement("div",{className:"timeline-detail"},l.createElement("button",{className:"secondary",disabled:u.busy,onClick:a},"Revert")):null,l.createElement(Et,{href:o.url,date:o.createdAt}))},"MergedEventView"),Cs=i(o=>l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},l.createElement("div",{className:"avatar-container"},l.createElement(yt,{for:o.actor})),l.createElement(Xe,{for:o.actor}),l.createElement("div",{className:"message"},"deleted the ",o.headRef," branch",Ue)),l.createElement(Et,{date:o.createdAt})),"HeadDeleteEventView"),Jn=i(o=>{const{source:a}=o;return l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},l.createElement("div",{className:"avatar-container"},l.createElement(yt,{for:o.actor})),l.createElement(Xe,{for:o.actor}),l.createElement("div",{className:"message"},"linked ",l.createElement("a",{href:a.extensionUrl},"#",a.number)," ",a.title,Ue,o.willCloseTarget?"which will close this issue":"")),l.createElement(Et,{date:o.createdAt}))},"CrossReferencedEventView");function Xi(o){return o.length===0?l.createElement(l.Fragment,null):o.length===1?o[0]:o.length===2?l.createElement(l.Fragment,null,o[0]," and ",o[1]):l.createElement(l.Fragment,null,o.slice(0,-1).map(a=>l.createElement(l.Fragment,null,a,", "))," and ",o[o.length-1])}i(Xi,"timeline_joinWithAnd");const Io=i(({event:o})=>{const{actor:a}=o,u=o.assignees||[],d=o.unassignees||[],f=Xi(u.map(C=>l.createElement(Xe,{key:C.id,for:C}))),p=Xi(d.map(C=>l.createElement(Xe,{key:C.id,for:C})));let g;return u.length>0&&d.length>0?g=l.createElement(l.Fragment,null,"assigned ",f," and unassigned ",p):u.length>0?g=l.createElement(l.Fragment,null,"assigned ",f):g=l.createElement(l.Fragment,null,"unassigned ",p),l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},l.createElement("div",{className:"avatar-container"},l.createElement(yt,{for:a})),l.createElement(Xe,{for:a}),l.createElement("div",{className:"message"},g)),l.createElement(Et,{date:o.createdAt}))},"AssignUnassignEventView"),xn=i(({event:o,isIssue:a})=>{const{actor:u,createdAt:d}=o;return l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},l.createElement("div",{className:"avatar-container"},l.createElement(yt,{for:u})),l.createElement(Xe,{for:u}),l.createElement("div",{className:"message"},a?"closed this issue":"closed this pull request")),l.createElement(Et,{date:d}))},"ClosedEventView"),Ho=i(({event:o,isIssue:a})=>{const{actor:u,createdAt:d}=o;return l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},l.createElement("div",{className:"avatar-container"},l.createElement(yt,{for:u})),l.createElement(Xe,{for:u}),l.createElement("div",{className:"message"},a?"reopened this issue":"reopened this pull request")),l.createElement(Et,{date:d}))},"ReopenedEventView"),er=i(o=>{const{createdAt:a,onBehalfOf:u,sessionLink:d}=o,{openSessionLog:f}=(0,l.useContext)(B),p=i(g=>{if(d){const C=g.ctrlKey||g.metaKey;f(d,C)}},"handleSessionLogClick");return l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},Qr,Ue,l.createElement("div",{className:"message"},"Copilot started work on behalf of ",l.createElement(Xe,{for:u}))),d?l.createElement("div",{className:"timeline-detail"},l.createElement("a",{onClick:p},l.createElement("button",{className:"secondary",title:"View session log (Ctrl/Cmd+Click to open in second editor group)"},"View session"))):null,l.createElement(Et,{date:a}))},"CopilotStartedEventView"),Fo=i(o=>{const{createdAt:a,onBehalfOf:u}=o;return l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},mi,Ue,l.createElement("div",{className:"message"},"Copilot finished work on behalf of ",l.createElement(Xe,{for:u}))),l.createElement(Et,{date:a}))},"CopilotFinishedEventView"),Ji=i(o=>{const{createdAt:a,onBehalfOf:u}=o,{openSessionLog:d}=(0,l.useContext)(B),f=i(p=>{const g=p.ctrlKey||p.metaKey;d(o.sessionLink,g)},"handleSessionLogClick");return l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"timeline-with-detail"},l.createElement("div",{className:"commit-message"},Kr,Ue,l.createElement("div",{className:"message"},"Copilot stopped work on behalf of ",l.createElement(Xe,{for:u})," due to an error")),l.createElement("div",{className:"commit-message-detail"},l.createElement("a",{onClick:f,title:"View session log (Ctrl/Cmd+Click to open in second editor group)"},"Copilot has encountered an error. See logs for additional details."))),l.createElement(Et,{date:a}))},"CopilotFinishedErrorEventView"),ws=i(o=>{const[a,u]=l.useState(window.matchMedia(o).matches);return l.useEffect(()=>{const d=window.matchMedia(o),f=i(()=>u(d.matches),"documentChangeHandler");return d.addEventListener("change",f),()=>{d.removeEventListener("change",f)}},[o]),a},"useMediaQuery"),xs=i(o=>{const a=ws("(max-width: 925px)");return l.createElement(l.Fragment,null,l.createElement("div",{id:"title",className:"title"},l.createElement("div",{className:"details"},l.createElement(Gl,{...o}))),a?l.createElement(l.Fragment,null,l.createElement(Oo,{...o}),l.createElement(el,{...o})):l.createElement(l.Fragment,null,l.createElement(el,{...o}),l.createElement(Oo,{...o})))},"Overview"),el=i(o=>l.createElement("div",{id:"main"},l.createElement("div",{id:"description"},l.createElement(zn,{isPRDescription:!0,comment:o})),l.createElement(Ao,{events:o.events,isIssue:o.isIssue}),l.createElement(Hi,{pr:o,isSimple:!1}),l.createElement(Lr,{...o})),"Main");function Es(){(0,ae.render)(l.createElement(Dr,null,o=>l.createElement(xs,{...o})),document.getElementById("app"))}i(Es,"main");function Dr({children:o}){const a=(0,l.useContext)(B),[u,d]=(0,l.useState)(a.pr);return(0,l.useEffect)(()=>{a.onchange=d,d(a.pr)},[]),window.onscroll=W(()=>{a.postMessage({command:"scroll",args:{scrollPosition:{x:window.scrollX,y:window.scrollY}}})},200),a.postMessage({command:"ready"}),a.postMessage({command:"pr.debug",args:"initialized "+(u?"with PR":"without PR")}),u?o(u):l.createElement("div",{className:"loading-indicator"},"Loading...")}i(Dr,"Root"),addEventListener("load",Es)})()})();
