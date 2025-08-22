var i1=Object.defineProperty;var i=(Dl,ei)=>i1(Dl,"name",{value:ei,configurable:!0});(()=>{var Dl={2410:(b,_,B)=>{"use strict";B.d(_,{A:i(()=>D,"A")});var K=B(31601),V=B.n(K),T=B(76314),g=B.n(T),p=g()(V());p.push([b.id,`/*---------------------------------------------------------------------------------------------
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
	width: 24px;
	position: relative;
}

button.select-right span {
	position: absolute;
	top: 2px;
	right: 4px;
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

svg path:first-of-type {
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
	display: flex;
}

.split {
	background-color: var(--vscode-button-background);
	border-top: 1px solid var(--vscode-button-border);
	border-bottom: 1px solid var(--vscode-button-border);
	padding: 4px 0;
}

.split .separator {
	height: 100%;
	width: 1px;
	background-color: var(--vscode-button-separator);
}

.split.disabled {
	opacity: 0.4;
}

.split.secondary {
	background-color: var(--vscode-button-secondaryBackground);
	border-top: 1px solid var(--vscode-button-secondaryBorder);
	border-bottom: 1px solid var(--vscode-button-secondaryBorder);
}

button.split-right {
	border-radius: 0 2px 2px 0;
	cursor: pointer;
	width: 24px;
	position: relative;
}

button.split-right:disabled {
	cursor: default;
}

button.split-right .icon {
	pointer-events: none;
	position: absolute;
	top: 4px;
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
	min-width: 0;
	margin: 0;
}

.dropdown-container.spreadable {
	flex-grow: 1;
	width: 100%;
}

button.inlined-dropdown {
	width: 100%;
	max-width: 150px;
	margin-right: 5px;
	display: inline-block;
	text-align: center;
}

.spinner {
	margin-top: 5px;
	margin-left: 5px;
}

.commit-spinner-inline {
	margin-left: 8px;
	display: inline-flex;
	align-items: center;
	vertical-align: middle;
	grid-column: none;
}

.commit-spinner-before {
	margin-right: 6px;
	display: inline-flex;
	align-items: center;
	vertical-align: middle;
}

.loading {
	animation: spinner-rotate 1s linear infinite;
}

@keyframes spinner-rotate {
	0% {
		transform: rotate(0deg);
	}

	100% {
		transform: rotate(360deg);
	}
}`,""]);const D=p},3554:(b,_,B)=>{"use strict";B.d(_,{A:i(()=>D,"A")});var K=B(31601),V=B.n(K),T=B(76314),g=B.n(T),p=g()(V());p.push([b.id,`/*---------------------------------------------------------------------------------------------
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

.loading-button {
	display: inline-flex;
	align-items: center;
	margin-right: 4px;
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
body .merged .merged-message>a {
	margin-right: 6px;
}

body .commit .commit-message>a {
	margin-right: 3px;
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

.overview-title {
	display: flex;
	align-items: center;
}

.overview-title h2 {
	font-size: 32px;
	margin-right: 6px;
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
	margin-top: 3px;
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

small-button {
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

.commit .commit-message a.message {
	cursor: pointer;
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
	cursor: pointer;
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
	flex-direction: row;
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

:not(.button-group) .dropdown-container {
	justify-content: right;
}

:not(.title-editing-form)>.form-actions {
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
		grid-template-columns: calc(50% - 10px) calc(50% - 10px);
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

.icon.copilot-icon {
	margin-right: 6px;
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
	margin-left: 8px;
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

.markdown-alert.markdown-alert-warning {
	border-left: .25em solid var(--vscode-editorWarning-foreground);
}

.markdown-alert.markdown-alert-warning .markdown-alert-title {
	color: var(--vscode-editorWarning-foreground);
}

.markdown-alert.markdown-alert-note {
	border-left: .25em solid var(--vscode-editorInfo-foreground);
}

.markdown-alert.markdown-alert-note .markdown-alert-title {
	color: var(--vscode-editorInfo-foreground);
}

.markdown-alert.markdown-alert-tip {
	border-left: .25em solid var(--vscode-testing-iconPassed);
}

.markdown-alert.markdown-alert-tip .markdown-alert-title {
	color: var(--vscode-testing-iconPassed);
}

.markdown-alert.markdown-alert-important {
	border-left: .25em solid var(--vscode-statusBar-debuggingBackground);
}

.markdown-alert.markdown-alert-important .markdown-alert-title {
	color: var(--vscode-statusBar-debuggingBackground);
}

.markdown-alert.markdown-alert-caution {
	border-left: .25em solid var(--vscode-editorError-foreground);
}

.markdown-alert.markdown-alert-caution .markdown-alert-title {
	color: var(--vscode-editorError-foreground);
}

.markdown-alert {
	padding: .5rem .5rem;
	margin-bottom: 1rem;
	color: inherit;
}

.markdown-alert .markdown-alert-title {
	display: flex;
	align-items: center;
	line-height: 1;
}

.markdown-alert-title svg {
	padding-right: 3px;
}

.markdown-alert>:first-child {
	margin-top: 0;
}

svg.octicon path {
	display: inline-block;
	overflow: visible !important;
	vertical-align: text-bottom;
	fill: currentColor;
}`,""]);const D=p},76314:b=>{"use strict";b.exports=function(_){var B=[];return B.toString=i(function(){return this.map(function(V){var T="",g=typeof V[5]!="undefined";return V[4]&&(T+="@supports (".concat(V[4],") {")),V[2]&&(T+="@media ".concat(V[2]," {")),g&&(T+="@layer".concat(V[5].length>0?" ".concat(V[5]):""," {")),T+=_(V),g&&(T+="}"),V[2]&&(T+="}"),V[4]&&(T+="}"),T}).join("")},"toString"),B.i=i(function(V,T,g,p,D){typeof V=="string"&&(V=[[null,V,void 0]]);var A={};if(g)for(var $=0;$<this.length;$++){var H=this[$][0];H!=null&&(A[H]=!0)}for(var X=0;X<V.length;X++){var Y=[].concat(V[X]);g&&A[Y[0]]||(typeof D!="undefined"&&(typeof Y[5]=="undefined"||(Y[1]="@layer".concat(Y[5].length>0?" ".concat(Y[5]):""," {").concat(Y[1],"}")),Y[5]=D),T&&(Y[2]&&(Y[1]="@media ".concat(Y[2]," {").concat(Y[1],"}")),Y[2]=T),p&&(Y[4]?(Y[1]="@supports (".concat(Y[4],") {").concat(Y[1],"}"),Y[4]=p):Y[4]="".concat(p)),B.push(Y))}},"i"),B}},31601:b=>{"use strict";b.exports=function(_){return _[1]}},74353:function(b){(function(_,B){b.exports=B()})(this,function(){"use strict";var _="millisecond",B="second",K="minute",V="hour",T="day",g="week",p="month",D="quarter",A="year",$="date",H=/^(\d{4})[-/]?(\d{1,2})?[-/]?(\d{0,2})[^0-9]*(\d{1,2})?:?(\d{1,2})?:?(\d{1,2})?[.:]?(\d+)?$/,X=/\[([^\]]+)]|Y{1,4}|M{1,4}|D{1,2}|d{1,4}|H{1,2}|h{1,2}|a|A|m{1,2}|s{1,2}|Z{1,2}|SSS/g,Y={name:"en",weekdays:"Sunday_Monday_Tuesday_Wednesday_Thursday_Friday_Saturday".split("_"),months:"January_February_March_April_May_June_July_August_September_October_November_December".split("_")},Oe=i(function(Z,O,I){var ne=String(Z);return!ne||ne.length>=O?Z:""+Array(O+1-ne.length).join(I)+Z},"$"),He={s:Oe,z:i(function(Z){var O=-Z.utcOffset(),I=Math.abs(O),ne=Math.floor(I/60),G=I%60;return(O<=0?"+":"-")+Oe(ne,2,"0")+":"+Oe(G,2,"0")},"z"),m:i(function Z(O,I){if(O.date()<I.date())return-Z(I,O);var ne=12*(I.year()-O.year())+(I.month()-O.month()),G=O.clone().add(ne,p),se=I-G<0,fe=O.clone().add(ne+(se?-1:1),p);return+(-(ne+(I-G)/(se?G-fe:fe-G))||0)},"t"),a:i(function(Z){return Z<0?Math.ceil(Z)||0:Math.floor(Z)},"a"),p:i(function(Z){return{M:p,y:A,w:g,d:T,D:$,h:V,m:K,s:B,ms:_,Q:D}[Z]||String(Z||"").toLowerCase().replace(/s$/,"")},"p"),u:i(function(Z){return Z===void 0},"u")},de="en",De={};De[de]=Y;var tt=i(function(Z){return Z instanceof oe},"m"),j=i(function(Z,O,I){var ne;if(!Z)return de;if(typeof Z=="string")De[Z]&&(ne=Z),O&&(De[Z]=O,ne=Z);else{var G=Z.name;De[G]=Z,ne=G}return!I&&ne&&(de=ne),ne||!I&&de},"D"),N=i(function(Z,O){if(tt(Z))return Z.clone();var I=typeof O=="object"?O:{};return I.date=Z,I.args=arguments,new oe(I)},"v"),l=He;l.l=j,l.i=tt,l.w=function(Z,O){return N(Z,{locale:O.$L,utc:O.$u,x:O.$x,$offset:O.$offset})};var oe=function(){function Z(I){this.$L=j(I.locale,null,!0),this.parse(I)}i(Z,"d");var O=Z.prototype;return O.parse=function(I){this.$d=function(ne){var G=ne.date,se=ne.utc;if(G===null)return new Date(NaN);if(l.u(G))return new Date;if(G instanceof Date)return new Date(G);if(typeof G=="string"&&!/Z$/i.test(G)){var fe=G.match(H);if(fe){var pe=fe[2]-1||0,ve=(fe[7]||"0").substring(0,3);return se?new Date(Date.UTC(fe[1],pe,fe[3]||1,fe[4]||0,fe[5]||0,fe[6]||0,ve)):new Date(fe[1],pe,fe[3]||1,fe[4]||0,fe[5]||0,fe[6]||0,ve)}}return new Date(G)}(I),this.$x=I.x||{},this.init()},O.init=function(){var I=this.$d;this.$y=I.getFullYear(),this.$M=I.getMonth(),this.$D=I.getDate(),this.$W=I.getDay(),this.$H=I.getHours(),this.$m=I.getMinutes(),this.$s=I.getSeconds(),this.$ms=I.getMilliseconds()},O.$utils=function(){return l},O.isValid=function(){return this.$d.toString()!=="Invalid Date"},O.isSame=function(I,ne){var G=N(I);return this.startOf(ne)<=G&&G<=this.endOf(ne)},O.isAfter=function(I,ne){return N(I)<this.startOf(ne)},O.isBefore=function(I,ne){return this.endOf(ne)<N(I)},O.$g=function(I,ne,G){return l.u(I)?this[ne]:this.set(G,I)},O.unix=function(){return Math.floor(this.valueOf()/1e3)},O.valueOf=function(){return this.$d.getTime()},O.startOf=function(I,ne){var G=this,se=!!l.u(ne)||ne,fe=l.p(I),pe=i(function(xe,Ue){var z=l.w(G.$u?Date.UTC(G.$y,Ue,xe):new Date(G.$y,Ue,xe),G);return se?z:z.endOf(T)},"$"),ve=i(function(xe,Ue){return l.w(G.toDate()[xe].apply(G.toDate("s"),(se?[0,0,0,0]:[23,59,59,999]).slice(Ue)),G)},"l"),Ae=this.$W,Ve=this.$M,re=this.$D,qe="set"+(this.$u?"UTC":"");switch(fe){case A:return se?pe(1,0):pe(31,11);case p:return se?pe(1,Ve):pe(0,Ve+1);case g:var at=this.$locale().weekStart||0,gt=(Ae<at?Ae+7:Ae)-at;return pe(se?re-gt:re+(6-gt),Ve);case T:case $:return ve(qe+"Hours",0);case V:return ve(qe+"Minutes",1);case K:return ve(qe+"Seconds",2);case B:return ve(qe+"Milliseconds",3);default:return this.clone()}},O.endOf=function(I){return this.startOf(I,!1)},O.$set=function(I,ne){var G,se=l.p(I),fe="set"+(this.$u?"UTC":""),pe=(G={},G[T]=fe+"Date",G[$]=fe+"Date",G[p]=fe+"Month",G[A]=fe+"FullYear",G[V]=fe+"Hours",G[K]=fe+"Minutes",G[B]=fe+"Seconds",G[_]=fe+"Milliseconds",G)[se],ve=se===T?this.$D+(ne-this.$W):ne;if(se===p||se===A){var Ae=this.clone().set($,1);Ae.$d[pe](ve),Ae.init(),this.$d=Ae.set($,Math.min(this.$D,Ae.daysInMonth())).$d}else pe&&this.$d[pe](ve);return this.init(),this},O.set=function(I,ne){return this.clone().$set(I,ne)},O.get=function(I){return this[l.p(I)]()},O.add=function(I,ne){var G,se=this;I=Number(I);var fe=l.p(ne),pe=i(function(Ve){var re=N(se);return l.w(re.date(re.date()+Math.round(Ve*I)),se)},"d");if(fe===p)return this.set(p,this.$M+I);if(fe===A)return this.set(A,this.$y+I);if(fe===T)return pe(1);if(fe===g)return pe(7);var ve=(G={},G[K]=6e4,G[V]=36e5,G[B]=1e3,G)[fe]||1,Ae=this.$d.getTime()+I*ve;return l.w(Ae,this)},O.subtract=function(I,ne){return this.add(-1*I,ne)},O.format=function(I){var ne=this;if(!this.isValid())return"Invalid Date";var G=I||"YYYY-MM-DDTHH:mm:ssZ",se=l.z(this),fe=this.$locale(),pe=this.$H,ve=this.$m,Ae=this.$M,Ve=fe.weekdays,re=fe.months,qe=i(function(Ue,z,Q,ue){return Ue&&(Ue[z]||Ue(ne,G))||Q[z].substr(0,ue)},"h"),at=i(function(Ue){return l.s(pe%12||12,Ue,"0")},"d"),gt=fe.meridiem||function(Ue,z,Q){var ue=Ue<12?"AM":"PM";return Q?ue.toLowerCase():ue},xe={YY:String(this.$y).slice(-2),YYYY:this.$y,M:Ae+1,MM:l.s(Ae+1,2,"0"),MMM:qe(fe.monthsShort,Ae,re,3),MMMM:qe(re,Ae),D:this.$D,DD:l.s(this.$D,2,"0"),d:String(this.$W),dd:qe(fe.weekdaysMin,this.$W,Ve,2),ddd:qe(fe.weekdaysShort,this.$W,Ve,3),dddd:Ve[this.$W],H:String(pe),HH:l.s(pe,2,"0"),h:at(1),hh:at(2),a:gt(pe,ve,!0),A:gt(pe,ve,!1),m:String(ve),mm:l.s(ve,2,"0"),s:String(this.$s),ss:l.s(this.$s,2,"0"),SSS:l.s(this.$ms,3,"0"),Z:se};return G.replace(X,function(Ue,z){return z||xe[Ue]||se.replace(":","")})},O.utcOffset=function(){return 15*-Math.round(this.$d.getTimezoneOffset()/15)},O.diff=function(I,ne,G){var se,fe=l.p(ne),pe=N(I),ve=6e4*(pe.utcOffset()-this.utcOffset()),Ae=this-pe,Ve=l.m(this,pe);return Ve=(se={},se[A]=Ve/12,se[p]=Ve,se[D]=Ve/3,se[g]=(Ae-ve)/6048e5,se[T]=(Ae-ve)/864e5,se[V]=Ae/36e5,se[K]=Ae/6e4,se[B]=Ae/1e3,se)[fe]||Ae,G?Ve:l.a(Ve)},O.daysInMonth=function(){return this.endOf(p).$D},O.$locale=function(){return De[this.$L]},O.locale=function(I,ne){if(!I)return this.$L;var G=this.clone(),se=j(I,ne,!0);return se&&(G.$L=se),G},O.clone=function(){return l.w(this.$d,this)},O.toDate=function(){return new Date(this.valueOf())},O.toJSON=function(){return this.isValid()?this.toISOString():null},O.toISOString=function(){return this.$d.toISOString()},O.toString=function(){return this.$d.toUTCString()},Z}(),q=oe.prototype;return N.prototype=q,[["$ms",_],["$s",B],["$m",K],["$H",V],["$W",T],["$M",p],["$y",A],["$D",$]].forEach(function(Z){q[Z[1]]=function(O){return this.$g(O,Z[0],Z[1])}}),N.extend=function(Z,O){return Z.$i||(Z(O,oe,N),Z.$i=!0),N},N.locale=j,N.isDayjs=tt,N.unix=function(Z){return N(1e3*Z)},N.en=De[de],N.Ls=De,N.p={},N})},6279:function(b){(function(_,B){b.exports=B()})(this,function(){"use strict";return function(_,B,K){_=_||{};var V=B.prototype,T={future:"in %s",past:"%s ago",s:"a few seconds",m:"a minute",mm:"%d minutes",h:"an hour",hh:"%d hours",d:"a day",dd:"%d days",M:"a month",MM:"%d months",y:"a year",yy:"%d years"};function g(D,A,$,H){return V.fromToBase(D,A,$,H)}i(g,"i"),K.en.relativeTime=T,V.fromToBase=function(D,A,$,H,X){for(var Y,Oe,He,de=$.$locale().relativeTime||T,De=_.thresholds||[{l:"s",r:44,d:"second"},{l:"m",r:89},{l:"mm",r:44,d:"minute"},{l:"h",r:89},{l:"hh",r:21,d:"hour"},{l:"d",r:35},{l:"dd",r:25,d:"day"},{l:"M",r:45},{l:"MM",r:10,d:"month"},{l:"y",r:17},{l:"yy",d:"year"}],tt=De.length,j=0;j<tt;j+=1){var N=De[j];N.d&&(Y=H?K(D).diff($,N.d,!0):$.diff(D,N.d,!0));var l=(_.rounding||Math.round)(Math.abs(Y));if(He=Y>0,l<=N.r||!N.r){l<=1&&j>0&&(N=De[j-1]);var oe=de[N.l];X&&(l=X(""+l)),Oe=typeof oe=="string"?oe.replace("%d",l):oe(l,A,N.l,He);break}}if(A)return Oe;var q=He?de.future:de.past;return typeof q=="function"?q(Oe):q.replace("%s",Oe)},V.to=function(D,A){return g(D,A,this,!0)},V.from=function(D,A){return g(D,A,this)};var p=i(function(D){return D.$u?K.utc():K()},"d");V.toNow=function(D){return this.to(p(this),D)},V.fromNow=function(D){return this.from(p(this),D)}}})},53581:function(b){(function(_,B){b.exports=B()})(this,function(){"use strict";return function(_,B,K){K.updateLocale=function(V,T){var g=K.Ls[V];if(g)return(T?Object.keys(T):[]).forEach(function(p){g[p]=T[p]}),g}}})},17334:b=>{function _(B,K,V){var T,g,p,D,A;K==null&&(K=100);function $(){var X=Date.now()-D;X<K&&X>=0?T=setTimeout($,K-X):(T=null,V||(A=B.apply(p,g),p=g=null))}i($,"later");var H=i(function(){p=this,g=arguments,D=Date.now();var X=V&&!T;return T||(T=setTimeout($,K)),X&&(A=B.apply(p,g),p=g=null),A},"debounced");return H.clear=function(){T&&(clearTimeout(T),T=null)},H.flush=function(){T&&(A=B.apply(p,g),p=g=null,clearTimeout(T),T=null)},H}i(_,"debounce"),_.debounce=_,b.exports=_},37007:b=>{"use strict";var _=typeof Reflect=="object"?Reflect:null,B=_&&typeof _.apply=="function"?_.apply:i(function(N,l,oe){return Function.prototype.apply.call(N,l,oe)},"ReflectApply"),K;_&&typeof _.ownKeys=="function"?K=_.ownKeys:Object.getOwnPropertySymbols?K=i(function(N){return Object.getOwnPropertyNames(N).concat(Object.getOwnPropertySymbols(N))},"ReflectOwnKeys"):K=i(function(N){return Object.getOwnPropertyNames(N)},"ReflectOwnKeys");function V(j){console&&console.warn&&console.warn(j)}i(V,"ProcessEmitWarning");var T=Number.isNaN||i(function(N){return N!==N},"NumberIsNaN");function g(){g.init.call(this)}i(g,"EventEmitter"),b.exports=g,b.exports.once=tt,g.EventEmitter=g,g.prototype._events=void 0,g.prototype._eventsCount=0,g.prototype._maxListeners=void 0;var p=10;function D(j){if(typeof j!="function")throw new TypeError('The "listener" argument must be of type Function. Received type '+typeof j)}i(D,"checkListener"),Object.defineProperty(g,"defaultMaxListeners",{enumerable:!0,get:i(function(){return p},"get"),set:i(function(j){if(typeof j!="number"||j<0||T(j))throw new RangeError('The value of "defaultMaxListeners" is out of range. It must be a non-negative number. Received '+j+".");p=j},"set")}),g.init=function(){(this._events===void 0||this._events===Object.getPrototypeOf(this)._events)&&(this._events=Object.create(null),this._eventsCount=0),this._maxListeners=this._maxListeners||void 0},g.prototype.setMaxListeners=i(function(N){if(typeof N!="number"||N<0||T(N))throw new RangeError('The value of "n" is out of range. It must be a non-negative number. Received '+N+".");return this._maxListeners=N,this},"setMaxListeners");function A(j){return j._maxListeners===void 0?g.defaultMaxListeners:j._maxListeners}i(A,"_getMaxListeners"),g.prototype.getMaxListeners=i(function(){return A(this)},"getMaxListeners"),g.prototype.emit=i(function(N){for(var l=[],oe=1;oe<arguments.length;oe++)l.push(arguments[oe]);var q=N==="error",Z=this._events;if(Z!==void 0)q=q&&Z.error===void 0;else if(!q)return!1;if(q){var O;if(l.length>0&&(O=l[0]),O instanceof Error)throw O;var I=new Error("Unhandled error."+(O?" ("+O.message+")":""));throw I.context=O,I}var ne=Z[N];if(ne===void 0)return!1;if(typeof ne=="function")B(ne,this,l);else for(var G=ne.length,se=He(ne,G),oe=0;oe<G;++oe)B(se[oe],this,l);return!0},"emit");function $(j,N,l,oe){var q,Z,O;if(D(l),Z=j._events,Z===void 0?(Z=j._events=Object.create(null),j._eventsCount=0):(Z.newListener!==void 0&&(j.emit("newListener",N,l.listener?l.listener:l),Z=j._events),O=Z[N]),O===void 0)O=Z[N]=l,++j._eventsCount;else if(typeof O=="function"?O=Z[N]=oe?[l,O]:[O,l]:oe?O.unshift(l):O.push(l),q=A(j),q>0&&O.length>q&&!O.warned){O.warned=!0;var I=new Error("Possible EventEmitter memory leak detected. "+O.length+" "+String(N)+" listeners added. Use emitter.setMaxListeners() to increase limit");I.name="MaxListenersExceededWarning",I.emitter=j,I.type=N,I.count=O.length,V(I)}return j}i($,"_addListener"),g.prototype.addListener=i(function(N,l){return $(this,N,l,!1)},"addListener"),g.prototype.on=g.prototype.addListener,g.prototype.prependListener=i(function(N,l){return $(this,N,l,!0)},"prependListener");function H(){if(!this.fired)return this.target.removeListener(this.type,this.wrapFn),this.fired=!0,arguments.length===0?this.listener.call(this.target):this.listener.apply(this.target,arguments)}i(H,"onceWrapper");function X(j,N,l){var oe={fired:!1,wrapFn:void 0,target:j,type:N,listener:l},q=H.bind(oe);return q.listener=l,oe.wrapFn=q,q}i(X,"_onceWrap"),g.prototype.once=i(function(N,l){return D(l),this.on(N,X(this,N,l)),this},"once"),g.prototype.prependOnceListener=i(function(N,l){return D(l),this.prependListener(N,X(this,N,l)),this},"prependOnceListener"),g.prototype.removeListener=i(function(N,l){var oe,q,Z,O,I;if(D(l),q=this._events,q===void 0)return this;if(oe=q[N],oe===void 0)return this;if(oe===l||oe.listener===l)--this._eventsCount===0?this._events=Object.create(null):(delete q[N],q.removeListener&&this.emit("removeListener",N,oe.listener||l));else if(typeof oe!="function"){for(Z=-1,O=oe.length-1;O>=0;O--)if(oe[O]===l||oe[O].listener===l){I=oe[O].listener,Z=O;break}if(Z<0)return this;Z===0?oe.shift():de(oe,Z),oe.length===1&&(q[N]=oe[0]),q.removeListener!==void 0&&this.emit("removeListener",N,I||l)}return this},"removeListener"),g.prototype.off=g.prototype.removeListener,g.prototype.removeAllListeners=i(function(N){var l,oe,q;if(oe=this._events,oe===void 0)return this;if(oe.removeListener===void 0)return arguments.length===0?(this._events=Object.create(null),this._eventsCount=0):oe[N]!==void 0&&(--this._eventsCount===0?this._events=Object.create(null):delete oe[N]),this;if(arguments.length===0){var Z=Object.keys(oe),O;for(q=0;q<Z.length;++q)O=Z[q],O!=="removeListener"&&this.removeAllListeners(O);return this.removeAllListeners("removeListener"),this._events=Object.create(null),this._eventsCount=0,this}if(l=oe[N],typeof l=="function")this.removeListener(N,l);else if(l!==void 0)for(q=l.length-1;q>=0;q--)this.removeListener(N,l[q]);return this},"removeAllListeners");function Y(j,N,l){var oe=j._events;if(oe===void 0)return[];var q=oe[N];return q===void 0?[]:typeof q=="function"?l?[q.listener||q]:[q]:l?De(q):He(q,q.length)}i(Y,"_listeners"),g.prototype.listeners=i(function(N){return Y(this,N,!0)},"listeners"),g.prototype.rawListeners=i(function(N){return Y(this,N,!1)},"rawListeners"),g.listenerCount=function(j,N){return typeof j.listenerCount=="function"?j.listenerCount(N):Oe.call(j,N)},g.prototype.listenerCount=Oe;function Oe(j){var N=this._events;if(N!==void 0){var l=N[j];if(typeof l=="function")return 1;if(l!==void 0)return l.length}return 0}i(Oe,"listenerCount"),g.prototype.eventNames=i(function(){return this._eventsCount>0?K(this._events):[]},"eventNames");function He(j,N){for(var l=new Array(N),oe=0;oe<N;++oe)l[oe]=j[oe];return l}i(He,"arrayClone");function de(j,N){for(;N+1<j.length;N++)j[N]=j[N+1];j.pop()}i(de,"spliceOne");function De(j){for(var N=new Array(j.length),l=0;l<N.length;++l)N[l]=j[l].listener||j[l];return N}i(De,"unwrapListeners");function tt(j,N){return new Promise(function(l,oe){function q(){Z!==void 0&&j.removeListener("error",Z),l([].slice.call(arguments))}i(q,"eventListener");var Z;N!=="error"&&(Z=i(function(I){j.removeListener(N,q),oe(I)},"errorListener"),j.once("error",Z)),j.once(N,q)})}i(tt,"once")},45228:b=>{"use strict";/*
object-assign
(c) Sindre Sorhus
@license MIT
*/var _=Object.getOwnPropertySymbols,B=Object.prototype.hasOwnProperty,K=Object.prototype.propertyIsEnumerable;function V(g){if(g==null)throw new TypeError("Object.assign cannot be called with null or undefined");return Object(g)}i(V,"toObject");function T(){try{if(!Object.assign)return!1;var g=new String("abc");if(g[5]="de",Object.getOwnPropertyNames(g)[0]==="5")return!1;for(var p={},D=0;D<10;D++)p["_"+String.fromCharCode(D)]=D;var A=Object.getOwnPropertyNames(p).map(function(H){return p[H]});if(A.join("")!=="0123456789")return!1;var $={};return"abcdefghijklmnopqrst".split("").forEach(function(H){$[H]=H}),Object.keys(Object.assign({},$)).join("")==="abcdefghijklmnopqrst"}catch{return!1}}i(T,"shouldUseNative"),b.exports=T()?Object.assign:function(g,p){for(var D,A=V(g),$,H=1;H<arguments.length;H++){D=Object(arguments[H]);for(var X in D)B.call(D,X)&&(A[X]=D[X]);if(_){$=_(D);for(var Y=0;Y<$.length;Y++)K.call(D,$[Y])&&(A[$[Y]]=D[$[Y]])}}return A}},57975:b=>{"use strict";function _(T){if(typeof T!="string")throw new TypeError("Path must be a string. Received "+JSON.stringify(T))}i(_,"assertPath");function B(T,g){for(var p="",D=0,A=-1,$=0,H,X=0;X<=T.length;++X){if(X<T.length)H=T.charCodeAt(X);else{if(H===47)break;H=47}if(H===47){if(!(A===X-1||$===1))if(A!==X-1&&$===2){if(p.length<2||D!==2||p.charCodeAt(p.length-1)!==46||p.charCodeAt(p.length-2)!==46){if(p.length>2){var Y=p.lastIndexOf("/");if(Y!==p.length-1){Y===-1?(p="",D=0):(p=p.slice(0,Y),D=p.length-1-p.lastIndexOf("/")),A=X,$=0;continue}}else if(p.length===2||p.length===1){p="",D=0,A=X,$=0;continue}}g&&(p.length>0?p+="/..":p="..",D=2)}else p.length>0?p+="/"+T.slice(A+1,X):p=T.slice(A+1,X),D=X-A-1;A=X,$=0}else H===46&&$!==-1?++$:$=-1}return p}i(B,"normalizeStringPosix");function K(T,g){var p=g.dir||g.root,D=g.base||(g.name||"")+(g.ext||"");return p?p===g.root?p+D:p+T+D:D}i(K,"_format");var V={resolve:i(function(){for(var g="",p=!1,D,A=arguments.length-1;A>=-1&&!p;A--){var $;A>=0?$=arguments[A]:(D===void 0&&(D=process.cwd()),$=D),_($),$.length!==0&&(g=$+"/"+g,p=$.charCodeAt(0)===47)}return g=B(g,!p),p?g.length>0?"/"+g:"/":g.length>0?g:"."},"resolve"),normalize:i(function(g){if(_(g),g.length===0)return".";var p=g.charCodeAt(0)===47,D=g.charCodeAt(g.length-1)===47;return g=B(g,!p),g.length===0&&!p&&(g="."),g.length>0&&D&&(g+="/"),p?"/"+g:g},"normalize"),isAbsolute:i(function(g){return _(g),g.length>0&&g.charCodeAt(0)===47},"isAbsolute"),join:i(function(){if(arguments.length===0)return".";for(var g,p=0;p<arguments.length;++p){var D=arguments[p];_(D),D.length>0&&(g===void 0?g=D:g+="/"+D)}return g===void 0?".":V.normalize(g)},"join"),relative:i(function(g,p){if(_(g),_(p),g===p||(g=V.resolve(g),p=V.resolve(p),g===p))return"";for(var D=1;D<g.length&&g.charCodeAt(D)===47;++D);for(var A=g.length,$=A-D,H=1;H<p.length&&p.charCodeAt(H)===47;++H);for(var X=p.length,Y=X-H,Oe=$<Y?$:Y,He=-1,de=0;de<=Oe;++de){if(de===Oe){if(Y>Oe){if(p.charCodeAt(H+de)===47)return p.slice(H+de+1);if(de===0)return p.slice(H+de)}else $>Oe&&(g.charCodeAt(D+de)===47?He=de:de===0&&(He=0));break}var De=g.charCodeAt(D+de),tt=p.charCodeAt(H+de);if(De!==tt)break;De===47&&(He=de)}var j="";for(de=D+He+1;de<=A;++de)(de===A||g.charCodeAt(de)===47)&&(j.length===0?j+="..":j+="/..");return j.length>0?j+p.slice(H+He):(H+=He,p.charCodeAt(H)===47&&++H,p.slice(H))},"relative"),_makeLong:i(function(g){return g},"_makeLong"),dirname:i(function(g){if(_(g),g.length===0)return".";for(var p=g.charCodeAt(0),D=p===47,A=-1,$=!0,H=g.length-1;H>=1;--H)if(p=g.charCodeAt(H),p===47){if(!$){A=H;break}}else $=!1;return A===-1?D?"/":".":D&&A===1?"//":g.slice(0,A)},"dirname"),basename:i(function(g,p){if(p!==void 0&&typeof p!="string")throw new TypeError('"ext" argument must be a string');_(g);var D=0,A=-1,$=!0,H;if(p!==void 0&&p.length>0&&p.length<=g.length){if(p.length===g.length&&p===g)return"";var X=p.length-1,Y=-1;for(H=g.length-1;H>=0;--H){var Oe=g.charCodeAt(H);if(Oe===47){if(!$){D=H+1;break}}else Y===-1&&($=!1,Y=H+1),X>=0&&(Oe===p.charCodeAt(X)?--X===-1&&(A=H):(X=-1,A=Y))}return D===A?A=Y:A===-1&&(A=g.length),g.slice(D,A)}else{for(H=g.length-1;H>=0;--H)if(g.charCodeAt(H)===47){if(!$){D=H+1;break}}else A===-1&&($=!1,A=H+1);return A===-1?"":g.slice(D,A)}},"basename"),extname:i(function(g){_(g);for(var p=-1,D=0,A=-1,$=!0,H=0,X=g.length-1;X>=0;--X){var Y=g.charCodeAt(X);if(Y===47){if(!$){D=X+1;break}continue}A===-1&&($=!1,A=X+1),Y===46?p===-1?p=X:H!==1&&(H=1):p!==-1&&(H=-1)}return p===-1||A===-1||H===0||H===1&&p===A-1&&p===D+1?"":g.slice(p,A)},"extname"),format:i(function(g){if(g===null||typeof g!="object")throw new TypeError('The "pathObject" argument must be of type Object. Received type '+typeof g);return K("/",g)},"format"),parse:i(function(g){_(g);var p={root:"",dir:"",base:"",ext:"",name:""};if(g.length===0)return p;var D=g.charCodeAt(0),A=D===47,$;A?(p.root="/",$=1):$=0;for(var H=-1,X=0,Y=-1,Oe=!0,He=g.length-1,de=0;He>=$;--He){if(D=g.charCodeAt(He),D===47){if(!Oe){X=He+1;break}continue}Y===-1&&(Oe=!1,Y=He+1),D===46?H===-1?H=He:de!==1&&(de=1):H!==-1&&(de=-1)}return H===-1||Y===-1||de===0||de===1&&H===Y-1&&H===X+1?Y!==-1&&(X===0&&A?p.base=p.name=g.slice(1,Y):p.base=p.name=g.slice(X,Y)):(X===0&&A?(p.name=g.slice(1,H),p.base=g.slice(1,Y)):(p.name=g.slice(X,H),p.base=g.slice(X,Y)),p.ext=g.slice(H,Y)),X>0?p.dir=g.slice(0,X-1):A&&(p.dir="/"),p},"parse"),sep:"/",delimiter:":",win32:null,posix:null};V.posix=V,b.exports=V},22551:(b,_,B)=>{"use strict";var K;/** @license React v16.14.0
 * react-dom.production.min.js
 *
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */var V=B(96540),T=B(45228),g=B(69982);function p(e){for(var t="https://reactjs.org/docs/error-decoder.html?invariant="+e,n=1;n<arguments.length;n++)t+="&args[]="+encodeURIComponent(arguments[n]);return"Minified React error #"+e+"; visit "+t+" for the full message or use the non-minified dev environment for full errors and additional helpful warnings."}if(i(p,"u"),!V)throw Error(p(227));function D(e,t,n,r,s,d,m,v,L){var S=Array.prototype.slice.call(arguments,3);try{t.apply(n,S)}catch(te){this.onError(te)}}i(D,"ba");var A=!1,$=null,H=!1,X=null,Y={onError:i(function(e){A=!0,$=e},"onError")};function Oe(e,t,n,r,s,d,m,v,L){A=!1,$=null,D.apply(Y,arguments)}i(Oe,"ja");function He(e,t,n,r,s,d,m,v,L){if(Oe.apply(this,arguments),A){if(A){var S=$;A=!1,$=null}else throw Error(p(198));H||(H=!0,X=S)}}i(He,"ka");var de=null,De=null,tt=null;function j(e,t,n){var r=e.type||"unknown-event";e.currentTarget=tt(n),He(r,t,void 0,e),e.currentTarget=null}i(j,"oa");var N=null,l={};function oe(){if(N)for(var e in l){var t=l[e],n=N.indexOf(e);if(!(-1<n))throw Error(p(96,e));if(!Z[n]){if(!t.extractEvents)throw Error(p(97,e));Z[n]=t,n=t.eventTypes;for(var r in n){var s=void 0,d=n[r],m=t,v=r;if(O.hasOwnProperty(v))throw Error(p(99,v));O[v]=d;var L=d.phasedRegistrationNames;if(L){for(s in L)L.hasOwnProperty(s)&&q(L[s],m,v);s=!0}else d.registrationName?(q(d.registrationName,m,v),s=!0):s=!1;if(!s)throw Error(p(98,r,e))}}}}i(oe,"ra");function q(e,t,n){if(I[e])throw Error(p(100,e));I[e]=t,ne[e]=t.eventTypes[n].dependencies}i(q,"ua");var Z=[],O={},I={},ne={};function G(e){var t=!1,n;for(n in e)if(e.hasOwnProperty(n)){var r=e[n];if(!l.hasOwnProperty(n)||l[n]!==r){if(l[n])throw Error(p(102,n));l[n]=r,t=!0}}t&&oe()}i(G,"xa");var se=!(typeof window=="undefined"||typeof window.document=="undefined"||typeof window.document.createElement=="undefined"),fe=null,pe=null,ve=null;function Ae(e){if(e=De(e)){if(typeof fe!="function")throw Error(p(280));var t=e.stateNode;t&&(t=de(t),fe(e.stateNode,e.type,t))}}i(Ae,"Ca");function Ve(e){pe?ve?ve.push(e):ve=[e]:pe=e}i(Ve,"Da");function re(){if(pe){var e=pe,t=ve;if(ve=pe=null,Ae(e),t)for(e=0;e<t.length;e++)Ae(t[e])}}i(re,"Ea");function qe(e,t){return e(t)}i(qe,"Fa");function at(e,t,n,r,s){return e(t,n,r,s)}i(at,"Ga");function gt(){}i(gt,"Ha");var xe=qe,Ue=!1,z=!1;function Q(){(pe!==null||ve!==null)&&(gt(),re())}i(Q,"La");function ue(e,t,n){if(z)return e(t,n);z=!0;try{return xe(e,t,n)}finally{z=!1,Q()}}i(ue,"Ma");var w=/^[:A-Z_a-z\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u02FF\u0370-\u037D\u037F-\u1FFF\u200C-\u200D\u2070-\u218F\u2C00-\u2FEF\u3001-\uD7FF\uF900-\uFDCF\uFDF0-\uFFFD][:A-Z_a-z\u00C0-\u00D6\u00D8-\u00F6\u00F8-\u02FF\u0370-\u037D\u037F-\u1FFF\u200C-\u200D\u2070-\u218F\u2C00-\u2FEF\u3001-\uD7FF\uF900-\uFDCF\uFDF0-\uFFFD\-.0-9\u00B7\u0300-\u036F\u203F-\u2040]*$/,P=Object.prototype.hasOwnProperty,he={},ke={};function be(e){return P.call(ke,e)?!0:P.call(he,e)?!1:w.test(e)?ke[e]=!0:(he[e]=!0,!1)}i(be,"Ra");function $e(e,t,n,r){if(n!==null&&n.type===0)return!1;switch(typeof t){case"function":case"symbol":return!0;case"boolean":return r?!1:n!==null?!n.acceptsBooleans:(e=e.toLowerCase().slice(0,5),e!=="data-"&&e!=="aria-");default:return!1}}i($e,"Sa");function xt(e,t,n,r){if(t===null||typeof t=="undefined"||$e(e,t,n,r))return!0;if(r)return!1;if(n!==null)switch(n.type){case 3:return!t;case 4:return t===!1;case 5:return isNaN(t);case 6:return isNaN(t)||1>t}return!1}i(xt,"Ta");function Le(e,t,n,r,s,d){this.acceptsBooleans=t===2||t===3||t===4,this.attributeName=r,this.attributeNamespace=s,this.mustUseProperty=n,this.propertyName=e,this.type=t,this.sanitizeURL=d}i(Le,"v");var ge={};"children dangerouslySetInnerHTML defaultValue defaultChecked innerHTML suppressContentEditableWarning suppressHydrationWarning style".split(" ").forEach(function(e){ge[e]=new Le(e,0,!1,e,null,!1)}),[["acceptCharset","accept-charset"],["className","class"],["htmlFor","for"],["httpEquiv","http-equiv"]].forEach(function(e){var t=e[0];ge[t]=new Le(t,1,!1,e[1],null,!1)}),["contentEditable","draggable","spellCheck","value"].forEach(function(e){ge[e]=new Le(e,2,!1,e.toLowerCase(),null,!1)}),["autoReverse","externalResourcesRequired","focusable","preserveAlpha"].forEach(function(e){ge[e]=new Le(e,2,!1,e,null,!1)}),"allowFullScreen async autoFocus autoPlay controls default defer disabled disablePictureInPicture formNoValidate hidden loop noModule noValidate open playsInline readOnly required reversed scoped seamless itemScope".split(" ").forEach(function(e){ge[e]=new Le(e,3,!1,e.toLowerCase(),null,!1)}),["checked","multiple","muted","selected"].forEach(function(e){ge[e]=new Le(e,3,!0,e,null,!1)}),["capture","download"].forEach(function(e){ge[e]=new Le(e,4,!1,e,null,!1)}),["cols","rows","size","span"].forEach(function(e){ge[e]=new Le(e,6,!1,e,null,!1)}),["rowSpan","start"].forEach(function(e){ge[e]=new Le(e,5,!1,e.toLowerCase(),null,!1)});var Ne=/[\-:]([a-z])/g;function Xr(e){return e[1].toUpperCase()}i(Xr,"Va"),"accent-height alignment-baseline arabic-form baseline-shift cap-height clip-path clip-rule color-interpolation color-interpolation-filters color-profile color-rendering dominant-baseline enable-background fill-opacity fill-rule flood-color flood-opacity font-family font-size font-size-adjust font-stretch font-style font-variant font-weight glyph-name glyph-orientation-horizontal glyph-orientation-vertical horiz-adv-x horiz-origin-x image-rendering letter-spacing lighting-color marker-end marker-mid marker-start overline-position overline-thickness paint-order panose-1 pointer-events rendering-intent shape-rendering stop-color stop-opacity strikethrough-position strikethrough-thickness stroke-dasharray stroke-dashoffset stroke-linecap stroke-linejoin stroke-miterlimit stroke-opacity stroke-width text-anchor text-decoration text-rendering underline-position underline-thickness unicode-bidi unicode-range units-per-em v-alphabetic v-hanging v-ideographic v-mathematical vector-effect vert-adv-y vert-origin-x vert-origin-y word-spacing writing-mode xmlns:xlink x-height".split(" ").forEach(function(e){var t=e.replace(Ne,Xr);ge[t]=new Le(t,1,!1,e,null,!1)}),"xlink:actuate xlink:arcrole xlink:role xlink:show xlink:title xlink:type".split(" ").forEach(function(e){var t=e.replace(Ne,Xr);ge[t]=new Le(t,1,!1,e,"http://www.w3.org/1999/xlink",!1)}),["xml:base","xml:lang","xml:space"].forEach(function(e){var t=e.replace(Ne,Xr);ge[t]=new Le(t,1,!1,e,"http://www.w3.org/XML/1998/namespace",!1)}),["tabIndex","crossOrigin"].forEach(function(e){ge[e]=new Le(e,1,!1,e.toLowerCase(),null,!1)}),ge.xlinkHref=new Le("xlinkHref",1,!1,"xlink:href","http://www.w3.org/1999/xlink",!0),["src","href","action","formAction"].forEach(function(e){ge[e]=new Le(e,1,!1,e.toLowerCase(),null,!0)});var kt=V.__SECRET_INTERNALS_DO_NOT_USE_OR_YOU_WILL_BE_FIRED;kt.hasOwnProperty("ReactCurrentDispatcher")||(kt.ReactCurrentDispatcher={current:null}),kt.hasOwnProperty("ReactCurrentBatchConfig")||(kt.ReactCurrentBatchConfig={suspense:null});function Jr(e,t,n,r){var s=ge.hasOwnProperty(t)?ge[t]:null,d=s!==null?s.type===0:r?!1:!(!(2<t.length)||t[0]!=="o"&&t[0]!=="O"||t[1]!=="n"&&t[1]!=="N");d||(xt(t,n,s,r)&&(n=null),r||s===null?be(t)&&(n===null?e.removeAttribute(t):e.setAttribute(t,""+n)):s.mustUseProperty?e[s.propertyName]=n===null?s.type===3?!1:"":n:(t=s.attributeName,r=s.attributeNamespace,n===null?e.removeAttribute(t):(s=s.type,n=s===3||s===4&&n===!0?"":""+n,r?e.setAttributeNS(r,t,n):e.setAttribute(t,n))))}i(Jr,"Xa");var ti=/^(.*)[\\\/]/,ut=typeof Symbol=="function"&&Symbol.for,Ce=ut?Symbol.for("react.element"):60103,zn=ut?Symbol.for("react.portal"):60106,Wt=ut?Symbol.for("react.fragment"):60107,Bn=ut?Symbol.for("react.strict_mode"):60108,Er=ut?Symbol.for("react.profiler"):60114,ni=ut?Symbol.for("react.provider"):60109,ri=ut?Symbol.for("react.context"):60110,Al=ut?Symbol.for("react.concurrent_mode"):60111,eo=ut?Symbol.for("react.forward_ref"):60112,kr=ut?Symbol.for("react.suspense"):60113,oi=ut?Symbol.for("react.suspense_list"):60120,to=ut?Symbol.for("react.memo"):60115,br=ut?Symbol.for("react.lazy"):60116,Il=ut?Symbol.for("react.block"):60121,ii=typeof Symbol=="function"&&Symbol.iterator;function jn(e){return e===null||typeof e!="object"?null:(e=ii&&e[ii]||e["@@iterator"],typeof e=="function"?e:null)}i(jn,"nb");function Hl(e){if(e._status===-1){e._status=0;var t=e._ctor;t=t(),e._result=t,t.then(function(n){e._status===0&&(n=n.default,e._status=1,e._result=n)},function(n){e._status===0&&(e._status=2,e._result=n)})}}i(Hl,"ob");function Rt(e){if(e==null)return null;if(typeof e=="function")return e.displayName||e.name||null;if(typeof e=="string")return e;switch(e){case Wt:return"Fragment";case zn:return"Portal";case Er:return"Profiler";case Bn:return"StrictMode";case kr:return"Suspense";case oi:return"SuspenseList"}if(typeof e=="object")switch(e.$$typeof){case ri:return"Context.Consumer";case ni:return"Context.Provider";case eo:var t=e.render;return t=t.displayName||t.name||"",e.displayName||(t!==""?"ForwardRef("+t+")":"ForwardRef");case to:return Rt(e.type);case Il:return Rt(e.render);case br:if(e=e._status===1?e._result:null)return Rt(e)}return null}i(Rt,"pb");function no(e){var t="";do{e:switch(e.tag){case 3:case 4:case 6:case 7:case 10:case 9:var n="";break e;default:var r=e._debugOwner,s=e._debugSource,d=Rt(e.type);n=null,r&&(n=Rt(r.type)),r=d,d="",s?d=" (at "+s.fileName.replace(ti,"")+":"+s.lineNumber+")":n&&(d=" (created by "+n+")"),n=`
    in `+(r||"Unknown")+d}t+=n,e=e.return}while(e);return t}i(no,"qb");function Ot(e){switch(typeof e){case"boolean":case"number":case"object":case"string":case"undefined":return e;default:return""}}i(Ot,"rb");function ro(e){var t=e.type;return(e=e.nodeName)&&e.toLowerCase()==="input"&&(t==="checkbox"||t==="radio")}i(ro,"sb");function Un(e){var t=ro(e)?"checked":"value",n=Object.getOwnPropertyDescriptor(e.constructor.prototype,t),r=""+e[t];if(!e.hasOwnProperty(t)&&typeof n!="undefined"&&typeof n.get=="function"&&typeof n.set=="function"){var s=n.get,d=n.set;return Object.defineProperty(e,t,{configurable:!0,get:i(function(){return s.call(this)},"get"),set:i(function(m){r=""+m,d.call(this,m)},"set")}),Object.defineProperty(e,t,{enumerable:n.enumerable}),{getValue:i(function(){return r},"getValue"),setValue:i(function(m){r=""+m},"setValue"),stopTracking:i(function(){e._valueTracker=null,delete e[t]},"stopTracking")}}}i(Un,"tb");function _t(e){e._valueTracker||(e._valueTracker=Un(e))}i(_t,"xb");function li(e){if(!e)return!1;var t=e._valueTracker;if(!t)return!0;var n=t.getValue(),r="";return e&&(r=ro(e)?e.checked?"true":"false":e.value),e=r,e!==n?(t.setValue(e),!0):!1}i(li,"yb");function si(e,t){var n=t.checked;return T({},t,{defaultChecked:void 0,defaultValue:void 0,value:void 0,checked:n!=null?n:e._wrapperState.initialChecked})}i(si,"zb");function Fl(e,t){var n=t.defaultValue==null?"":t.defaultValue,r=t.checked!=null?t.checked:t.defaultChecked;n=Ot(t.value!=null?t.value:n),e._wrapperState={initialChecked:r,initialValue:n,controlled:t.type==="checkbox"||t.type==="radio"?t.checked!=null:t.value!=null}}i(Fl,"Ab");function Vl(e,t){t=t.checked,t!=null&&Jr(e,"checked",t,!1)}i(Vl,"Bb");function ai(e,t){Vl(e,t);var n=Ot(t.value),r=t.type;if(n!=null)r==="number"?(n===0&&e.value===""||e.value!=n)&&(e.value=""+n):e.value!==""+n&&(e.value=""+n);else if(r==="submit"||r==="reset"){e.removeAttribute("value");return}t.hasOwnProperty("value")?ui(e,t.type,n):t.hasOwnProperty("defaultValue")&&ui(e,t.type,Ot(t.defaultValue)),t.checked==null&&t.defaultChecked!=null&&(e.defaultChecked=!!t.defaultChecked)}i(ai,"Cb");function $l(e,t,n){if(t.hasOwnProperty("value")||t.hasOwnProperty("defaultValue")){var r=t.type;if(!(r!=="submit"&&r!=="reset"||t.value!==void 0&&t.value!==null))return;t=""+e._wrapperState.initialValue,n||t===e.value||(e.value=t),e.defaultValue=t}n=e.name,n!==""&&(e.name=""),e.defaultChecked=!!e._wrapperState.initialChecked,n!==""&&(e.name=n)}i($l,"Eb");function ui(e,t,n){(t!=="number"||e.ownerDocument.activeElement!==e)&&(n==null?e.defaultValue=""+e._wrapperState.initialValue:e.defaultValue!==""+n&&(e.defaultValue=""+n))}i(ui,"Db");function Ea(e){var t="";return V.Children.forEach(e,function(n){n!=null&&(t+=n)}),t}i(Ea,"Fb");function ci(e,t){return e=T({children:void 0},t),(t=Ea(t.children))&&(e.children=t),e}i(ci,"Gb");function Wn(e,t,n,r){if(e=e.options,t){t={};for(var s=0;s<n.length;s++)t["$"+n[s]]=!0;for(n=0;n<e.length;n++)s=t.hasOwnProperty("$"+e[n].value),e[n].selected!==s&&(e[n].selected=s),s&&r&&(e[n].defaultSelected=!0)}else{for(n=""+Ot(n),t=null,s=0;s<e.length;s++){if(e[s].value===n){e[s].selected=!0,r&&(e[s].defaultSelected=!0);return}t!==null||e[s].disabled||(t=e[s])}t!==null&&(t.selected=!0)}}i(Wn,"Hb");function di(e,t){if(t.dangerouslySetInnerHTML!=null)throw Error(p(91));return T({},t,{value:void 0,defaultValue:void 0,children:""+e._wrapperState.initialValue})}i(di,"Ib");function fi(e,t){var n=t.value;if(n==null){if(n=t.children,t=t.defaultValue,n!=null){if(t!=null)throw Error(p(92));if(Array.isArray(n)){if(!(1>=n.length))throw Error(p(93));n=n[0]}t=n}t==null&&(t=""),n=t}e._wrapperState={initialValue:Ot(n)}}i(fi,"Jb");function mi(e,t){var n=Ot(t.value),r=Ot(t.defaultValue);n!=null&&(n=""+n,n!==e.value&&(e.value=n),t.defaultValue==null&&e.defaultValue!==n&&(e.defaultValue=n)),r!=null&&(e.defaultValue=""+r)}i(mi,"Kb");function pi(e){var t=e.textContent;t===e._wrapperState.initialValue&&t!==""&&t!==null&&(e.value=t)}i(pi,"Lb");var hi={html:"http://www.w3.org/1999/xhtml",mathml:"http://www.w3.org/1998/Math/MathML",svg:"http://www.w3.org/2000/svg"};function vi(e){switch(e){case"svg":return"http://www.w3.org/2000/svg";case"math":return"http://www.w3.org/1998/Math/MathML";default:return"http://www.w3.org/1999/xhtml"}}i(vi,"Nb");function oo(e,t){return e==null||e==="http://www.w3.org/1999/xhtml"?vi(t):e==="http://www.w3.org/2000/svg"&&t==="foreignObject"?"http://www.w3.org/1999/xhtml":e}i(oo,"Ob");var rn,gi=function(e){return typeof MSApp!="undefined"&&MSApp.execUnsafeLocalFunction?function(t,n,r,s){MSApp.execUnsafeLocalFunction(function(){return e(t,n,r,s)})}:e}(function(e,t){if(e.namespaceURI!==hi.svg||"innerHTML"in e)e.innerHTML=t;else{for(rn=rn||document.createElement("div"),rn.innerHTML="<svg>"+t.valueOf().toString()+"</svg>",t=rn.firstChild;e.firstChild;)e.removeChild(e.firstChild);for(;t.firstChild;)e.appendChild(t.firstChild)}});function Zn(e,t){if(t){var n=e.firstChild;if(n&&n===e.lastChild&&n.nodeType===3){n.nodeValue=t;return}}e.textContent=t}i(Zn,"Rb");function _r(e,t){var n={};return n[e.toLowerCase()]=t.toLowerCase(),n["Webkit"+e]="webkit"+t,n["Moz"+e]="moz"+t,n}i(_r,"Sb");var xn={animationend:_r("Animation","AnimationEnd"),animationiteration:_r("Animation","AnimationIteration"),animationstart:_r("Animation","AnimationStart"),transitionend:_r("Transition","TransitionEnd")},qn={},rt={};se&&(rt=document.createElement("div").style,"AnimationEvent"in window||(delete xn.animationend.animation,delete xn.animationiteration.animation,delete xn.animationstart.animation),"TransitionEvent"in window||delete xn.transitionend.transition);function Lr(e){if(qn[e])return qn[e];if(!xn[e])return e;var t=xn[e],n;for(n in t)if(t.hasOwnProperty(n)&&n in rt)return qn[e]=t[n];return e}i(Lr,"Wb");var zl=Lr("animationend"),yi=Lr("animationiteration"),Qn=Lr("animationstart"),Ci=Lr("transitionend"),Kn="abort canplay canplaythrough durationchange emptied encrypted ended error loadeddata loadedmetadata loadstart pause play playing progress ratechange seeked seeking stalled suspend timeupdate volumechange waiting".split(" "),wi=new(typeof WeakMap=="function"?WeakMap:Map);function io(e){var t=wi.get(e);return t===void 0&&(t=new Map,wi.set(e,t)),t}i(io,"cc");function on(e){var t=e,n=e;if(e.alternate)for(;t.return;)t=t.return;else{e=t;do t=e,t.effectTag&1026&&(n=t.return),e=t.return;while(e)}return t.tag===3?n:null}i(on,"dc");function xi(e){if(e.tag===13){var t=e.memoizedState;if(t===null&&(e=e.alternate,e!==null&&(t=e.memoizedState)),t!==null)return t.dehydrated}return null}i(xi,"ec");function lo(e){if(on(e)!==e)throw Error(p(188))}i(lo,"fc");function Bl(e){var t=e.alternate;if(!t){if(t=on(e),t===null)throw Error(p(188));return t!==e?null:e}for(var n=e,r=t;;){var s=n.return;if(s===null)break;var d=s.alternate;if(d===null){if(r=s.return,r!==null){n=r;continue}break}if(s.child===d.child){for(d=s.child;d;){if(d===n)return lo(s),e;if(d===r)return lo(s),t;d=d.sibling}throw Error(p(188))}if(n.return!==r.return)n=s,r=d;else{for(var m=!1,v=s.child;v;){if(v===n){m=!0,n=s,r=d;break}if(v===r){m=!0,r=s,n=d;break}v=v.sibling}if(!m){for(v=d.child;v;){if(v===n){m=!0,n=d,r=s;break}if(v===r){m=!0,r=d,n=s;break}v=v.sibling}if(!m)throw Error(p(189))}}if(n.alternate!==r)throw Error(p(190))}if(n.tag!==3)throw Error(p(188));return n.stateNode.current===n?e:t}i(Bl,"gc");function jl(e){if(e=Bl(e),!e)return null;for(var t=e;;){if(t.tag===5||t.tag===6)return t;if(t.child)t.child.return=t,t=t.child;else{if(t===e)break;for(;!t.sibling;){if(!t.return||t.return===e)return null;t=t.return}t.sibling.return=t.return,t=t.sibling}}return null}i(jl,"hc");function ln(e,t){if(t==null)throw Error(p(30));return e==null?t:Array.isArray(e)?Array.isArray(t)?(e.push.apply(e,t),e):(e.push(t),e):Array.isArray(t)?[e].concat(t):[e,t]}i(ln,"ic");function Ei(e,t,n){Array.isArray(e)?e.forEach(t,n):e&&t.call(n,e)}i(Ei,"jc");var En=null;function Ul(e){if(e){var t=e._dispatchListeners,n=e._dispatchInstances;if(Array.isArray(t))for(var r=0;r<t.length&&!e.isPropagationStopped();r++)j(e,t[r],n[r]);else t&&j(e,t,n);e._dispatchListeners=null,e._dispatchInstances=null,e.isPersistent()||e.constructor.release(e)}}i(Ul,"lc");function Sr(e){if(e!==null&&(En=ln(En,e)),e=En,En=null,e){if(Ei(e,Ul),En)throw Error(p(95));if(H)throw e=X,H=!1,X=null,e}}i(Sr,"mc");function Ke(e){return e=e.target||e.srcElement||window,e.correspondingUseElement&&(e=e.correspondingUseElement),e.nodeType===3?e.parentNode:e}i(Ke,"nc");function Wl(e){if(!se)return!1;e="on"+e;var t=e in document;return t||(t=document.createElement("div"),t.setAttribute(e,"return;"),t=typeof t[e]=="function"),t}i(Wl,"oc");var so=[];function Zl(e){e.topLevelType=null,e.nativeEvent=null,e.targetInst=null,e.ancestors.length=0,10>so.length&&so.push(e)}i(Zl,"qc");function ql(e,t,n,r){if(so.length){var s=so.pop();return s.topLevelType=e,s.eventSystemFlags=r,s.nativeEvent=t,s.targetInst=n,s}return{topLevelType:e,eventSystemFlags:r,nativeEvent:t,targetInst:n,ancestors:[]}}i(ql,"rc");function ki(e){var t=e.targetInst,n=t;do{if(!n){e.ancestors.push(n);break}var r=n;if(r.tag===3)r=r.stateNode.containerInfo;else{for(;r.return;)r=r.return;r=r.tag!==3?null:r.stateNode.containerInfo}if(!r)break;t=n.tag,t!==5&&t!==6||e.ancestors.push(n),n=nr(r)}while(n);for(n=0;n<e.ancestors.length;n++){t=e.ancestors[n];var s=Ke(e.nativeEvent);r=e.topLevelType;var d=e.nativeEvent,m=e.eventSystemFlags;n===0&&(m|=64);for(var v=null,L=0;L<Z.length;L++){var S=Z[L];S&&(S=S.extractEvents(r,t,d,s,m))&&(v=ln(v,S))}Sr(v)}}i(ki,"sc");function bi(e,t,n){if(!n.has(e)){switch(e){case"scroll":ct(t,"scroll",!0);break;case"focus":case"blur":ct(t,"focus",!0),ct(t,"blur",!0),n.set("blur",null),n.set("focus",null);break;case"cancel":case"close":Wl(e)&&ct(t,e,!0);break;case"invalid":case"submit":case"reset":break;default:Kn.indexOf(e)===-1&&Qe(e,t)}n.set(e,null)}}i(bi,"uc");var ao,Yn,_i,uo=!1,Vt=[],sn=null,an=null,Lt=null,un=new Map,kn=new Map,bn=[],co="mousedown mouseup touchcancel touchend touchstart auxclick dblclick pointercancel pointerdown pointerup dragend dragstart drop compositionend compositionstart keydown keypress keyup input textInput close cancel copy cut paste click change contextmenu reset submit".split(" "),Li="focus blur dragenter dragleave mouseover mouseout pointerover pointerout gotpointercapture lostpointercapture".split(" ");function Ql(e,t){var n=io(t);co.forEach(function(r){bi(r,t,n)}),Li.forEach(function(r){bi(r,t,n)})}i(Ql,"Jc");function fo(e,t,n,r,s){return{blockedOn:e,topLevelType:t,eventSystemFlags:n|32,nativeEvent:s,container:r}}i(fo,"Kc");function mo(e,t){switch(e){case"focus":case"blur":sn=null;break;case"dragenter":case"dragleave":an=null;break;case"mouseover":case"mouseout":Lt=null;break;case"pointerover":case"pointerout":un.delete(t.pointerId);break;case"gotpointercapture":case"lostpointercapture":kn.delete(t.pointerId)}}i(mo,"Lc");function cn(e,t,n,r,s,d){return e===null||e.nativeEvent!==d?(e=fo(t,n,r,s,d),t!==null&&(t=rr(t),t!==null&&Yn(t)),e):(e.eventSystemFlags|=r,e)}i(cn,"Mc");function Si(e,t,n,r,s){switch(t){case"focus":return sn=cn(sn,e,t,n,r,s),!0;case"dragenter":return an=cn(an,e,t,n,r,s),!0;case"mouseover":return Lt=cn(Lt,e,t,n,r,s),!0;case"pointerover":var d=s.pointerId;return un.set(d,cn(un.get(d)||null,e,t,n,r,s)),!0;case"gotpointercapture":return d=s.pointerId,kn.set(d,cn(kn.get(d)||null,e,t,n,r,s)),!0}return!1}i(Si,"Oc");function po(e){var t=nr(e.target);if(t!==null){var n=on(t);if(n!==null){if(t=n.tag,t===13){if(t=xi(n),t!==null){e.blockedOn=t,g.unstable_runWithPriority(e.priority,function(){_i(n)});return}}else if(t===3&&n.stateNode.hydrate){e.blockedOn=n.tag===3?n.stateNode.containerInfo:null;return}}}e.blockedOn=null}i(po,"Pc");function Gn(e){if(e.blockedOn!==null)return!1;var t=Ln(e.topLevelType,e.eventSystemFlags,e.container,e.nativeEvent);if(t!==null){var n=rr(t);return n!==null&&Yn(n),e.blockedOn=t,!1}return!0}i(Gn,"Qc");function ho(e,t,n){Gn(e)&&n.delete(t)}i(ho,"Sc");function vo(){for(uo=!1;0<Vt.length;){var e=Vt[0];if(e.blockedOn!==null){e=rr(e.blockedOn),e!==null&&ao(e);break}var t=Ln(e.topLevelType,e.eventSystemFlags,e.container,e.nativeEvent);t!==null?e.blockedOn=t:Vt.shift()}sn!==null&&Gn(sn)&&(sn=null),an!==null&&Gn(an)&&(an=null),Lt!==null&&Gn(Lt)&&(Lt=null),un.forEach(ho),kn.forEach(ho)}i(vo,"Tc");function dn(e,t){e.blockedOn===t&&(e.blockedOn=null,uo||(uo=!0,g.unstable_scheduleCallback(g.unstable_NormalPriority,vo)))}i(dn,"Uc");function Xn(e){function t(s){return dn(s,e)}if(i(t,"b"),0<Vt.length){dn(Vt[0],e);for(var n=1;n<Vt.length;n++){var r=Vt[n];r.blockedOn===e&&(r.blockedOn=null)}}for(sn!==null&&dn(sn,e),an!==null&&dn(an,e),Lt!==null&&dn(Lt,e),un.forEach(t),kn.forEach(t),n=0;n<bn.length;n++)r=bn[n],r.blockedOn===e&&(r.blockedOn=null);for(;0<bn.length&&(n=bn[0],n.blockedOn===null);)po(n),n.blockedOn===null&&bn.shift()}i(Xn,"Vc");var Ti={},Tr=new Map,Mi=new Map,_n=["abort","abort",zl,"animationEnd",yi,"animationIteration",Qn,"animationStart","canplay","canPlay","canplaythrough","canPlayThrough","durationchange","durationChange","emptied","emptied","encrypted","encrypted","ended","ended","error","error","gotpointercapture","gotPointerCapture","load","load","loadeddata","loadedData","loadedmetadata","loadedMetadata","loadstart","loadStart","lostpointercapture","lostPointerCapture","playing","playing","progress","progress","seeking","seeking","stalled","stalled","suspend","suspend","timeupdate","timeUpdate",Ci,"transitionEnd","waiting","waiting"];function Mr(e,t){for(var n=0;n<e.length;n+=2){var r=e[n],s=e[n+1],d="on"+(s[0].toUpperCase()+s.slice(1));d={phasedRegistrationNames:{bubbled:d,captured:d+"Capture"},dependencies:[r],eventPriority:t},Mi.set(r,t),Tr.set(r,d),Ti[s]=d}}i(Mr,"ad"),Mr("blur blur cancel cancel click click close close contextmenu contextMenu copy copy cut cut auxclick auxClick dblclick doubleClick dragend dragEnd dragstart dragStart drop drop focus focus input input invalid invalid keydown keyDown keypress keyPress keyup keyUp mousedown mouseDown mouseup mouseUp paste paste pause pause play play pointercancel pointerCancel pointerdown pointerDown pointerup pointerUp ratechange rateChange reset reset seeked seeked submit submit touchcancel touchCancel touchend touchEnd touchstart touchStart volumechange volumeChange".split(" "),0),Mr("drag drag dragenter dragEnter dragexit dragExit dragleave dragLeave dragover dragOver mousemove mouseMove mouseout mouseOut mouseover mouseOver pointermove pointerMove pointerout pointerOut pointerover pointerOver scroll scroll toggle toggle touchmove touchMove wheel wheel".split(" "),1),Mr(_n,2);for(var Kl="change selectionchange textInput compositionstart compositionend compositionupdate".split(" "),Ni=0;Ni<Kl.length;Ni++)Mi.set(Kl[Ni],0);var ka=g.unstable_UserBlockingPriority,St=g.unstable_runWithPriority,go=!0;function Qe(e,t){ct(t,e,!1)}i(Qe,"F");function ct(e,t,n){var r=Mi.get(t);switch(r===void 0?2:r){case 0:r=ht.bind(null,t,1,e);break;case 1:r=Yl.bind(null,t,1,e);break;default:r=fn.bind(null,t,1,e)}n?e.addEventListener(t,r,!0):e.addEventListener(t,r,!1)}i(ct,"vc");function ht(e,t,n,r){Ue||gt();var s=fn,d=Ue;Ue=!0;try{at(s,e,t,n,r)}finally{(Ue=d)||Q()}}i(ht,"gd");function Yl(e,t,n,r){St(ka,fn.bind(null,e,t,n,r))}i(Yl,"hd");function fn(e,t,n,r){if(go)if(0<Vt.length&&-1<co.indexOf(e))e=fo(null,e,t,n,r),Vt.push(e);else{var s=Ln(e,t,n,r);if(s===null)mo(e,r);else if(-1<co.indexOf(e))e=fo(s,e,t,n,r),Vt.push(e);else if(!Si(s,e,t,n,r)){mo(e,r),e=ql(e,r,null,t);try{ue(ki,e)}finally{Zl(e)}}}}i(fn,"id");function Ln(e,t,n,r){if(n=Ke(r),n=nr(n),n!==null){var s=on(n);if(s===null)n=null;else{var d=s.tag;if(d===13){if(n=xi(s),n!==null)return n;n=null}else if(d===3){if(s.stateNode.hydrate)return s.tag===3?s.stateNode.containerInfo:null;n=null}else s!==n&&(n=null)}}e=ql(e,r,n,t);try{ue(ki,e)}finally{Zl(e)}return null}i(Ln,"Rc");var Jn={animationIterationCount:!0,borderImageOutset:!0,borderImageSlice:!0,borderImageWidth:!0,boxFlex:!0,boxFlexGroup:!0,boxOrdinalGroup:!0,columnCount:!0,columns:!0,flex:!0,flexGrow:!0,flexPositive:!0,flexShrink:!0,flexNegative:!0,flexOrder:!0,gridArea:!0,gridRow:!0,gridRowEnd:!0,gridRowSpan:!0,gridRowStart:!0,gridColumn:!0,gridColumnEnd:!0,gridColumnSpan:!0,gridColumnStart:!0,fontWeight:!0,lineClamp:!0,lineHeight:!0,opacity:!0,order:!0,orphans:!0,tabSize:!0,widows:!0,zIndex:!0,zoom:!0,fillOpacity:!0,floodOpacity:!0,stopOpacity:!0,strokeDasharray:!0,strokeDashoffset:!0,strokeMiterlimit:!0,strokeOpacity:!0,strokeWidth:!0},Gl=["Webkit","ms","Moz","O"];Object.keys(Jn).forEach(function(e){Gl.forEach(function(t){t=t+e.charAt(0).toUpperCase()+e.substring(1),Jn[t]=Jn[e]})});function Pi(e,t,n){return t==null||typeof t=="boolean"||t===""?"":n||typeof t!="number"||t===0||Jn.hasOwnProperty(e)&&Jn[e]?(""+t).trim():t+"px"}i(Pi,"ld");function yo(e,t){e=e.style;for(var n in t)if(t.hasOwnProperty(n)){var r=n.indexOf("--")===0,s=Pi(n,t[n],r);n==="float"&&(n="cssFloat"),r?e.setProperty(n,s):e[n]=s}}i(yo,"md");var Xl=T({menuitem:!0},{area:!0,base:!0,br:!0,col:!0,embed:!0,hr:!0,img:!0,input:!0,keygen:!0,link:!0,meta:!0,param:!0,source:!0,track:!0,wbr:!0});function Co(e,t){if(t){if(Xl[e]&&(t.children!=null||t.dangerouslySetInnerHTML!=null))throw Error(p(137,e,""));if(t.dangerouslySetInnerHTML!=null){if(t.children!=null)throw Error(p(60));if(!(typeof t.dangerouslySetInnerHTML=="object"&&"__html"in t.dangerouslySetInnerHTML))throw Error(p(61))}if(t.style!=null&&typeof t.style!="object")throw Error(p(62,""))}}i(Co,"od");function wo(e,t){if(e.indexOf("-")===-1)return typeof t.is=="string";switch(e){case"annotation-xml":case"color-profile":case"font-face":case"font-face-src":case"font-face-uri":case"font-face-format":case"font-face-name":case"missing-glyph":return!1;default:return!0}}i(wo,"pd");var Ri=hi.html;function Dt(e,t){e=e.nodeType===9||e.nodeType===11?e:e.ownerDocument;var n=io(e);t=ne[t];for(var r=0;r<t.length;r++)bi(t[r],e,n)}i(Dt,"rd");function er(){}i(er,"sd");function tr(e){if(e=e||(typeof document!="undefined"?document:void 0),typeof e=="undefined")return null;try{return e.activeElement||e.body}catch{return e.body}}i(tr,"td");function xo(e){for(;e&&e.firstChild;)e=e.firstChild;return e}i(xo,"ud");function Jl(e,t){var n=xo(e);e=0;for(var r;n;){if(n.nodeType===3){if(r=e+n.textContent.length,e<=t&&r>=t)return{node:n,offset:t-e};e=r}e:{for(;n;){if(n.nextSibling){n=n.nextSibling;break e}n=n.parentNode}n=void 0}n=xo(n)}}i(Jl,"vd");function Eo(e,t){return e&&t?e===t?!0:e&&e.nodeType===3?!1:t&&t.nodeType===3?Eo(e,t.parentNode):"contains"in e?e.contains(t):e.compareDocumentPosition?!!(e.compareDocumentPosition(t)&16):!1:!1}i(Eo,"wd");function Oi(){for(var e=window,t=tr();t instanceof e.HTMLIFrameElement;){try{var n=typeof t.contentWindow.location.href=="string"}catch{n=!1}if(n)e=t.contentWindow;else break;t=tr(e.document)}return t}i(Oi,"xd");function mn(e){var t=e&&e.nodeName&&e.nodeName.toLowerCase();return t&&(t==="input"&&(e.type==="text"||e.type==="search"||e.type==="tel"||e.type==="url"||e.type==="password")||t==="textarea"||e.contentEditable==="true")}i(mn,"yd");var Nr="$",Di="/$",ko="$?",bo="$!",_o=null,Lo=null;function Ai(e,t){switch(e){case"button":case"input":case"select":case"textarea":return!!t.autoFocus}return!1}i(Ai,"Fd");function So(e,t){return e==="textarea"||e==="option"||e==="noscript"||typeof t.children=="string"||typeof t.children=="number"||typeof t.dangerouslySetInnerHTML=="object"&&t.dangerouslySetInnerHTML!==null&&t.dangerouslySetInnerHTML.__html!=null}i(So,"Gd");var To=typeof setTimeout=="function"?setTimeout:void 0,es=typeof clearTimeout=="function"?clearTimeout:void 0;function pn(e){for(;e!=null;e=e.nextSibling){var t=e.nodeType;if(t===1||t===3)break}return e}i(pn,"Jd");function Ii(e){e=e.previousSibling;for(var t=0;e;){if(e.nodeType===8){var n=e.data;if(n===Nr||n===bo||n===ko){if(t===0)return e;t--}else n===Di&&t++}e=e.previousSibling}return null}i(Ii,"Kd");var Mo=Math.random().toString(36).slice(2),Zt="__reactInternalInstance$"+Mo,Pr="__reactEventHandlers$"+Mo,qt="__reactContainere$"+Mo;function nr(e){var t=e[Zt];if(t)return t;for(var n=e.parentNode;n;){if(t=n[qt]||n[Zt]){if(n=t.alternate,t.child!==null||n!==null&&n.child!==null)for(e=Ii(e);e!==null;){if(n=e[Zt])return n;e=Ii(e)}return t}e=n,n=e.parentNode}return null}i(nr,"tc");function rr(e){return e=e[Zt]||e[qt],!e||e.tag!==5&&e.tag!==6&&e.tag!==13&&e.tag!==3?null:e}i(rr,"Nc");function hn(e){if(e.tag===5||e.tag===6)return e.stateNode;throw Error(p(33))}i(hn,"Pd");function No(e){return e[Pr]||null}i(No,"Qd");function $t(e){do e=e.return;while(e&&e.tag!==5);return e||null}i($t,"Rd");function ot(e,t){var n=e.stateNode;if(!n)return null;var r=de(n);if(!r)return null;n=r[t];e:switch(t){case"onClick":case"onClickCapture":case"onDoubleClick":case"onDoubleClickCapture":case"onMouseDown":case"onMouseDownCapture":case"onMouseMove":case"onMouseMoveCapture":case"onMouseUp":case"onMouseUpCapture":case"onMouseEnter":(r=!r.disabled)||(e=e.type,r=!(e==="button"||e==="input"||e==="select"||e==="textarea")),e=!r;break e;default:e=!1}if(e)return null;if(n&&typeof n!="function")throw Error(p(231,t,typeof n));return n}i(ot,"Sd");function Rr(e,t,n){(t=ot(e,n.dispatchConfig.phasedRegistrationNames[t]))&&(n._dispatchListeners=ln(n._dispatchListeners,t),n._dispatchInstances=ln(n._dispatchInstances,e))}i(Rr,"Td");function ts(e){if(e&&e.dispatchConfig.phasedRegistrationNames){for(var t=e._targetInst,n=[];t;)n.push(t),t=$t(t);for(t=n.length;0<t--;)Rr(n[t],"captured",e);for(t=0;t<n.length;t++)Rr(n[t],"bubbled",e)}}i(ts,"Ud");function Po(e,t,n){e&&n&&n.dispatchConfig.registrationName&&(t=ot(e,n.dispatchConfig.registrationName))&&(n._dispatchListeners=ln(n._dispatchListeners,t),n._dispatchInstances=ln(n._dispatchInstances,e))}i(Po,"Vd");function ns(e){e&&e.dispatchConfig.registrationName&&Po(e._targetInst,null,e)}i(ns,"Wd");function Sn(e){Ei(e,ts)}i(Sn,"Xd");var Qt=null,Ro=null,Or=null;function Hi(){if(Or)return Or;var e,t=Ro,n=t.length,r,s="value"in Qt?Qt.value:Qt.textContent,d=s.length;for(e=0;e<n&&t[e]===s[e];e++);var m=n-e;for(r=1;r<=m&&t[n-r]===s[d-r];r++);return Or=s.slice(e,1<r?1-r:void 0)}i(Hi,"ae");function Dr(){return!0}i(Dr,"be");function Ar(){return!1}i(Ar,"ce");function Et(e,t,n,r){this.dispatchConfig=e,this._targetInst=t,this.nativeEvent=n,e=this.constructor.Interface;for(var s in e)e.hasOwnProperty(s)&&((t=e[s])?this[s]=t(n):s==="target"?this.target=r:this[s]=n[s]);return this.isDefaultPrevented=(n.defaultPrevented!=null?n.defaultPrevented:n.returnValue===!1)?Dr:Ar,this.isPropagationStopped=Ar,this}i(Et,"G"),T(Et.prototype,{preventDefault:i(function(){this.defaultPrevented=!0;var e=this.nativeEvent;e&&(e.preventDefault?e.preventDefault():typeof e.returnValue!="unknown"&&(e.returnValue=!1),this.isDefaultPrevented=Dr)},"preventDefault"),stopPropagation:i(function(){var e=this.nativeEvent;e&&(e.stopPropagation?e.stopPropagation():typeof e.cancelBubble!="unknown"&&(e.cancelBubble=!0),this.isPropagationStopped=Dr)},"stopPropagation"),persist:i(function(){this.isPersistent=Dr},"persist"),isPersistent:Ar,destructor:i(function(){var e=this.constructor.Interface,t;for(t in e)this[t]=null;this.nativeEvent=this._targetInst=this.dispatchConfig=null,this.isPropagationStopped=this.isDefaultPrevented=Ar,this._dispatchInstances=this._dispatchListeners=null},"destructor")}),Et.Interface={type:null,target:null,currentTarget:i(function(){return null},"currentTarget"),eventPhase:null,bubbles:null,cancelable:null,timeStamp:i(function(e){return e.timeStamp||Date.now()},"timeStamp"),defaultPrevented:null,isTrusted:null},Et.extend=function(e){function t(){}i(t,"b");function n(){return r.apply(this,arguments)}i(n,"c");var r=this;t.prototype=r.prototype;var s=new t;return T(s,n.prototype),n.prototype=s,n.prototype.constructor=n,n.Interface=T({},r.Interface,e),n.extend=r.extend,Fi(n),n},Fi(Et);function rs(e,t,n,r){if(this.eventPool.length){var s=this.eventPool.pop();return this.call(s,e,t,n,r),s}return new this(e,t,n,r)}i(rs,"ee");function os(e){if(!(e instanceof this))throw Error(p(279));e.destructor(),10>this.eventPool.length&&this.eventPool.push(e)}i(os,"fe");function Fi(e){e.eventPool=[],e.getPooled=rs,e.release=os}i(Fi,"de");var ba=Et.extend({data:null}),is=Et.extend({data:null}),ls=[9,13,27,32],Oo=se&&"CompositionEvent"in window,or=null;se&&"documentMode"in document&&(or=document.documentMode);var ss=se&&"TextEvent"in window&&!or,as=se&&(!Oo||or&&8<or&&11>=or),Vi=" ",zt={beforeInput:{phasedRegistrationNames:{bubbled:"onBeforeInput",captured:"onBeforeInputCapture"},dependencies:["compositionend","keypress","textInput","paste"]},compositionEnd:{phasedRegistrationNames:{bubbled:"onCompositionEnd",captured:"onCompositionEndCapture"},dependencies:"blur compositionend keydown keypress keyup mousedown".split(" ")},compositionStart:{phasedRegistrationNames:{bubbled:"onCompositionStart",captured:"onCompositionStartCapture"},dependencies:"blur compositionstart keydown keypress keyup mousedown".split(" ")},compositionUpdate:{phasedRegistrationNames:{bubbled:"onCompositionUpdate",captured:"onCompositionUpdateCapture"},dependencies:"blur compositionupdate keydown keypress keyup mousedown".split(" ")}},$i=!1;function zi(e,t){switch(e){case"keyup":return ls.indexOf(t.keyCode)!==-1;case"keydown":return t.keyCode!==229;case"keypress":case"mousedown":case"blur":return!0;default:return!1}}i(zi,"qe");function Bi(e){return e=e.detail,typeof e=="object"&&"data"in e?e.data:null}i(Bi,"re");var Bt=!1;function ji(e,t){switch(e){case"compositionend":return Bi(t);case"keypress":return t.which!==32?null:($i=!0,Vi);case"textInput":return e=t.data,e===Vi&&$i?null:e;default:return null}}i(ji,"te");function us(e,t){if(Bt)return e==="compositionend"||!Oo&&zi(e,t)?(e=Hi(),Or=Ro=Qt=null,Bt=!1,e):null;switch(e){case"paste":return null;case"keypress":if(!(t.ctrlKey||t.altKey||t.metaKey)||t.ctrlKey&&t.altKey){if(t.char&&1<t.char.length)return t.char;if(t.which)return String.fromCharCode(t.which)}return null;case"compositionend":return as&&t.locale!=="ko"?null:t.data;default:return null}}i(us,"ue");var cs={eventTypes:zt,extractEvents:i(function(e,t,n,r){var s;if(Oo)e:{switch(e){case"compositionstart":var d=zt.compositionStart;break e;case"compositionend":d=zt.compositionEnd;break e;case"compositionupdate":d=zt.compositionUpdate;break e}d=void 0}else Bt?zi(e,n)&&(d=zt.compositionEnd):e==="keydown"&&n.keyCode===229&&(d=zt.compositionStart);return d?(as&&n.locale!=="ko"&&(Bt||d!==zt.compositionStart?d===zt.compositionEnd&&Bt&&(s=Hi()):(Qt=r,Ro="value"in Qt?Qt.value:Qt.textContent,Bt=!0)),d=ba.getPooled(d,t,n,r),s?d.data=s:(s=Bi(n),s!==null&&(d.data=s)),Sn(d),s=d):s=null,(e=ss?ji(e,n):us(e,n))?(t=is.getPooled(zt.beforeInput,t,n,r),t.data=e,Sn(t)):t=null,s===null?t:t===null?s:[s,t]},"extractEvents")},Ui={color:!0,date:!0,datetime:!0,"datetime-local":!0,email:!0,month:!0,number:!0,password:!0,range:!0,search:!0,tel:!0,text:!0,time:!0,url:!0,week:!0};function Wi(e){var t=e&&e.nodeName&&e.nodeName.toLowerCase();return t==="input"?!!Ui[e.type]:t==="textarea"}i(Wi,"xe");var Zi={change:{phasedRegistrationNames:{bubbled:"onChange",captured:"onChangeCapture"},dependencies:"blur change click focus input keydown keyup selectionchange".split(" ")}};function qi(e,t,n){return e=Et.getPooled(Zi.change,e,t,n),e.type="change",Ve(n),Sn(e),e}i(qi,"ze");var Ir=null,Tn=null;function ds(e){Sr(e)}i(ds,"Ce");function Hr(e){var t=hn(e);if(li(t))return e}i(Hr,"De");function fs(e,t){if(e==="change")return t}i(fs,"Ee");var Fr=!1;se&&(Fr=Wl("input")&&(!document.documentMode||9<document.documentMode));function Do(){Ir&&(Ir.detachEvent("onpropertychange",Qi),Tn=Ir=null)}i(Do,"Ge");function Qi(e){if(e.propertyName==="value"&&Hr(Tn))if(e=qi(Tn,e,Ke(e)),Ue)Sr(e);else{Ue=!0;try{qe(ds,e)}finally{Ue=!1,Q()}}}i(Qi,"He");function ms(e,t,n){e==="focus"?(Do(),Ir=t,Tn=n,Ir.attachEvent("onpropertychange",Qi)):e==="blur"&&Do()}i(ms,"Ie");function ps(e){if(e==="selectionchange"||e==="keyup"||e==="keydown")return Hr(Tn)}i(ps,"Je");function Ki(e,t){if(e==="click")return Hr(t)}i(Ki,"Ke");function Vr(e,t){if(e==="input"||e==="change")return Hr(t)}i(Vr,"Le");var hs={eventTypes:Zi,_isInputEventSupported:Fr,extractEvents:i(function(e,t,n,r){var s=t?hn(t):window,d=s.nodeName&&s.nodeName.toLowerCase();if(d==="select"||d==="input"&&s.type==="file")var m=fs;else if(Wi(s))if(Fr)m=Vr;else{m=ps;var v=ms}else(d=s.nodeName)&&d.toLowerCase()==="input"&&(s.type==="checkbox"||s.type==="radio")&&(m=Ki);if(m&&(m=m(e,t)))return qi(m,n,r);v&&v(e,s,t),e==="blur"&&(e=s._wrapperState)&&e.controlled&&s.type==="number"&&ui(s,"number",s.value)},"extractEvents")},vn=Et.extend({view:null,detail:null}),Yi={Alt:"altKey",Control:"ctrlKey",Meta:"metaKey",Shift:"shiftKey"};function vs(e){var t=this.nativeEvent;return t.getModifierState?t.getModifierState(e):(e=Yi[e])?!!t[e]:!1}i(vs,"Pe");function Ao(){return vs}i(Ao,"Qe");var Io=0,Gi=0,gs=!1,ys=!1,ir=vn.extend({screenX:null,screenY:null,clientX:null,clientY:null,pageX:null,pageY:null,ctrlKey:null,shiftKey:null,altKey:null,metaKey:null,getModifierState:Ao,button:null,buttons:null,relatedTarget:i(function(e){return e.relatedTarget||(e.fromElement===e.srcElement?e.toElement:e.fromElement)},"relatedTarget"),movementX:i(function(e){if("movementX"in e)return e.movementX;var t=Io;return Io=e.screenX,gs?e.type==="mousemove"?e.screenX-t:0:(gs=!0,0)},"movementX"),movementY:i(function(e){if("movementY"in e)return e.movementY;var t=Gi;return Gi=e.screenY,ys?e.type==="mousemove"?e.screenY-t:0:(ys=!0,0)},"movementY")}),Cs=ir.extend({pointerId:null,width:null,height:null,pressure:null,tangentialPressure:null,tiltX:null,tiltY:null,twist:null,pointerType:null,isPrimary:null}),lr={mouseEnter:{registrationName:"onMouseEnter",dependencies:["mouseout","mouseover"]},mouseLeave:{registrationName:"onMouseLeave",dependencies:["mouseout","mouseover"]},pointerEnter:{registrationName:"onPointerEnter",dependencies:["pointerout","pointerover"]},pointerLeave:{registrationName:"onPointerLeave",dependencies:["pointerout","pointerover"]}},ws={eventTypes:lr,extractEvents:i(function(e,t,n,r,s){var d=e==="mouseover"||e==="pointerover",m=e==="mouseout"||e==="pointerout";if(d&&!(s&32)&&(n.relatedTarget||n.fromElement)||!m&&!d)return null;if(d=r.window===r?r:(d=r.ownerDocument)?d.defaultView||d.parentWindow:window,m){if(m=t,t=(t=n.relatedTarget||n.toElement)?nr(t):null,t!==null){var v=on(t);(t!==v||t.tag!==5&&t.tag!==6)&&(t=null)}}else m=null;if(m===t)return null;if(e==="mouseout"||e==="mouseover")var L=ir,S=lr.mouseLeave,te=lr.mouseEnter,ie="mouse";else(e==="pointerout"||e==="pointerover")&&(L=Cs,S=lr.pointerLeave,te=lr.pointerEnter,ie="pointer");if(e=m==null?d:hn(m),d=t==null?d:hn(t),S=L.getPooled(S,m,n,r),S.type=ie+"leave",S.target=e,S.relatedTarget=d,n=L.getPooled(te,t,n,r),n.type=ie+"enter",n.target=d,n.relatedTarget=e,r=m,ie=t,r&&ie)e:{for(L=r,te=ie,m=0,e=L;e;e=$t(e))m++;for(e=0,t=te;t;t=$t(t))e++;for(;0<m-e;)L=$t(L),m--;for(;0<e-m;)te=$t(te),e--;for(;m--;){if(L===te||L===te.alternate)break e;L=$t(L),te=$t(te)}L=null}else L=null;for(te=L,L=[];r&&r!==te&&(m=r.alternate,!(m!==null&&m===te));)L.push(r),r=$t(r);for(r=[];ie&&ie!==te&&(m=ie.alternate,!(m!==null&&m===te));)r.push(ie),ie=$t(ie);for(ie=0;ie<L.length;ie++)Po(L[ie],"bubbled",S);for(ie=r.length;0<ie--;)Po(r[ie],"captured",n);return s&64?[S,n]:[S]},"extractEvents")};function xs(e,t){return e===t&&(e!==0||1/e===1/t)||e!==e&&t!==t}i(xs,"Ze");var gn=typeof Object.is=="function"?Object.is:xs,Xi=Object.prototype.hasOwnProperty;function sr(e,t){if(gn(e,t))return!0;if(typeof e!="object"||e===null||typeof t!="object"||t===null)return!1;var n=Object.keys(e),r=Object.keys(t);if(n.length!==r.length)return!1;for(r=0;r<n.length;r++)if(!Xi.call(t,n[r])||!gn(e[n[r]],t[n[r]]))return!1;return!0}i(sr,"bf");var Ji=se&&"documentMode"in document&&11>=document.documentMode,el={select:{phasedRegistrationNames:{bubbled:"onSelect",captured:"onSelectCapture"},dependencies:"blur contextmenu dragend focus keydown keyup mousedown mouseup selectionchange".split(" ")}},ar=null,Ho=null,ur=null,Fo=!1;function tl(e,t){var n=t.window===t?t.document:t.nodeType===9?t:t.ownerDocument;return Fo||ar==null||ar!==tr(n)?null:(n=ar,"selectionStart"in n&&mn(n)?n={start:n.selectionStart,end:n.selectionEnd}:(n=(n.ownerDocument&&n.ownerDocument.defaultView||window).getSelection(),n={anchorNode:n.anchorNode,anchorOffset:n.anchorOffset,focusNode:n.focusNode,focusOffset:n.focusOffset}),ur&&sr(ur,n)?null:(ur=n,e=Et.getPooled(el.select,Ho,e,t),e.type="select",e.target=ar,Sn(e),e))}i(tl,"jf");var Es={eventTypes:el,extractEvents:i(function(e,t,n,r,s,d){if(s=d||(r.window===r?r.document:r.nodeType===9?r:r.ownerDocument),!(d=!s)){e:{s=io(s),d=ne.onSelect;for(var m=0;m<d.length;m++)if(!s.has(d[m])){s=!1;break e}s=!0}d=!s}if(d)return null;switch(s=t?hn(t):window,e){case"focus":(Wi(s)||s.contentEditable==="true")&&(ar=s,Ho=t,ur=null);break;case"blur":ur=Ho=ar=null;break;case"mousedown":Fo=!0;break;case"contextmenu":case"mouseup":case"dragend":return Fo=!1,tl(n,r);case"selectionchange":if(Ji)break;case"keydown":case"keyup":return tl(n,r)}return null},"extractEvents")},ks=Et.extend({animationName:null,elapsedTime:null,pseudoElement:null}),bs=Et.extend({clipboardData:i(function(e){return"clipboardData"in e?e.clipboardData:window.clipboardData},"clipboardData")}),_s=vn.extend({relatedTarget:null});function $r(e){var t=e.keyCode;return"charCode"in e?(e=e.charCode,e===0&&t===13&&(e=13)):e=t,e===10&&(e=13),32<=e||e===13?e:0}i($r,"of");var Ls={Esc:"Escape",Spacebar:" ",Left:"ArrowLeft",Up:"ArrowUp",Right:"ArrowRight",Down:"ArrowDown",Del:"Delete",Win:"OS",Menu:"ContextMenu",Apps:"ContextMenu",Scroll:"ScrollLock",MozPrintableKey:"Unidentified"},Ss={8:"Backspace",9:"Tab",12:"Clear",13:"Enter",16:"Shift",17:"Control",18:"Alt",19:"Pause",20:"CapsLock",27:"Escape",32:" ",33:"PageUp",34:"PageDown",35:"End",36:"Home",37:"ArrowLeft",38:"ArrowUp",39:"ArrowRight",40:"ArrowDown",45:"Insert",46:"Delete",112:"F1",113:"F2",114:"F3",115:"F4",116:"F5",117:"F6",118:"F7",119:"F8",120:"F9",121:"F10",122:"F11",123:"F12",144:"NumLock",145:"ScrollLock",224:"Meta"},nl=vn.extend({key:i(function(e){if(e.key){var t=Ls[e.key]||e.key;if(t!=="Unidentified")return t}return e.type==="keypress"?(e=$r(e),e===13?"Enter":String.fromCharCode(e)):e.type==="keydown"||e.type==="keyup"?Ss[e.keyCode]||"Unidentified":""},"key"),location:null,ctrlKey:null,shiftKey:null,altKey:null,metaKey:null,repeat:null,locale:null,getModifierState:Ao,charCode:i(function(e){return e.type==="keypress"?$r(e):0},"charCode"),keyCode:i(function(e){return e.type==="keydown"||e.type==="keyup"?e.keyCode:0},"keyCode"),which:i(function(e){return e.type==="keypress"?$r(e):e.type==="keydown"||e.type==="keyup"?e.keyCode:0},"which")}),rl=ir.extend({dataTransfer:null}),Ts=vn.extend({touches:null,targetTouches:null,changedTouches:null,altKey:null,metaKey:null,ctrlKey:null,shiftKey:null,getModifierState:Ao}),Ms=Et.extend({propertyName:null,elapsedTime:null,pseudoElement:null}),Ns=ir.extend({deltaX:i(function(e){return"deltaX"in e?e.deltaX:"wheelDeltaX"in e?-e.wheelDeltaX:0},"deltaX"),deltaY:i(function(e){return"deltaY"in e?e.deltaY:"wheelDeltaY"in e?-e.wheelDeltaY:"wheelDelta"in e?-e.wheelDelta:0},"deltaY"),deltaZ:null,deltaMode:null}),Ps={eventTypes:Ti,extractEvents:i(function(e,t,n,r){var s=Tr.get(e);if(!s)return null;switch(e){case"keypress":if($r(n)===0)return null;case"keydown":case"keyup":e=nl;break;case"blur":case"focus":e=_s;break;case"click":if(n.button===2)return null;case"auxclick":case"dblclick":case"mousedown":case"mousemove":case"mouseup":case"mouseout":case"mouseover":case"contextmenu":e=ir;break;case"drag":case"dragend":case"dragenter":case"dragexit":case"dragleave":case"dragover":case"dragstart":case"drop":e=rl;break;case"touchcancel":case"touchend":case"touchmove":case"touchstart":e=Ts;break;case zl:case yi:case Qn:e=ks;break;case Ci:e=Ms;break;case"scroll":e=vn;break;case"wheel":e=Ns;break;case"copy":case"cut":case"paste":e=bs;break;case"gotpointercapture":case"lostpointercapture":case"pointercancel":case"pointerdown":case"pointermove":case"pointerout":case"pointerover":case"pointerup":e=Cs;break;default:e=Et}return t=e.getPooled(s,t,n,r),Sn(t),t},"extractEvents")};if(N)throw Error(p(101));N=Array.prototype.slice.call("ResponderEventPlugin SimpleEventPlugin EnterLeaveEventPlugin ChangeEventPlugin SelectEventPlugin BeforeInputEventPlugin".split(" ")),oe();var Rs=rr;de=No,De=Rs,tt=hn,G({SimpleEventPlugin:Ps,EnterLeaveEventPlugin:ws,ChangeEventPlugin:hs,SelectEventPlugin:Es,BeforeInputEventPlugin:cs});var Vo=[],Mn=-1;function We(e){0>Mn||(e.current=Vo[Mn],Vo[Mn]=null,Mn--)}i(We,"H");function Ge(e,t){Mn++,Vo[Mn]=e.current,e.current=t}i(Ge,"I");var Kt={},it={current:Kt},nt={current:!1},jt=Kt;function Yt(e,t){var n=e.type.contextTypes;if(!n)return Kt;var r=e.stateNode;if(r&&r.__reactInternalMemoizedUnmaskedChildContext===t)return r.__reactInternalMemoizedMaskedChildContext;var s={},d;for(d in n)s[d]=t[d];return r&&(e=e.stateNode,e.__reactInternalMemoizedUnmaskedChildContext=t,e.__reactInternalMemoizedMaskedChildContext=s),s}i(Yt,"Cf");function dt(e){return e=e.childContextTypes,e!=null}i(dt,"L");function Nn(){We(nt),We(it)}i(Nn,"Df");function zr(e,t,n){if(it.current!==Kt)throw Error(p(168));Ge(it,t),Ge(nt,n)}i(zr,"Ef");function Br(e,t,n){var r=e.stateNode;if(e=t.childContextTypes,typeof r.getChildContext!="function")return n;r=r.getChildContext();for(var s in r)if(!(s in e))throw Error(p(108,Rt(t)||"Unknown",s));return T({},n,{},r)}i(Br,"Ff");function Pn(e){return e=(e=e.stateNode)&&e.__reactInternalMemoizedMergedChildContext||Kt,jt=it.current,Ge(it,e),Ge(nt,nt.current),!0}i(Pn,"Gf");function Gt(e,t,n){var r=e.stateNode;if(!r)throw Error(p(169));n?(e=Br(e,t,jt),r.__reactInternalMemoizedMergedChildContext=e,We(nt),We(it),Ge(it,e)):We(nt),Ge(nt,n)}i(Gt,"Hf");var $o=g.unstable_runWithPriority,cr=g.unstable_scheduleCallback,jr=g.unstable_cancelCallback,Ur=g.unstable_requestPaint,o=g.unstable_now,a=g.unstable_getCurrentPriorityLevel,u=g.unstable_ImmediatePriority,c=g.unstable_UserBlockingPriority,f=g.unstable_NormalPriority,h=g.unstable_LowPriority,y=g.unstable_IdlePriority,C={},E=g.unstable_shouldYield,R=Ur!==void 0?Ur:function(){},F=null,W=null,ae=!1,Se=o(),me=1e4>Se?o:function(){return o()-Se};function Pe(){switch(a()){case u:return 99;case c:return 98;case f:return 97;case h:return 96;case y:return 95;default:throw Error(p(332))}}i(Pe,"ag");function we(e){switch(e){case 99:return u;case 98:return c;case 97:return f;case 96:return h;case 95:return y;default:throw Error(p(332))}}i(we,"bg");function Te(e,t){return e=we(e),$o(e,t)}i(Te,"cg");function Ze(e,t,n){return e=we(e),cr(e,t,n)}i(Ze,"dg");function Be(e){return F===null?(F=[e],W=cr(u,Xe)):F.push(e),C}i(Be,"eg");function Ye(){if(W!==null){var e=W;W=null,jr(e)}Xe()}i(Ye,"gg");function Xe(){if(!ae&&F!==null){ae=!0;var e=0;try{var t=F;Te(99,function(){for(;e<t.length;e++){var n=t[e];do n=n(!0);while(n!==null)}}),F=null}catch(n){throw F!==null&&(F=F.slice(e+1)),cr(u,Ye),n}finally{ae=!1}}}i(Xe,"fg");function _e(e,t,n){return n/=10,1073741821-(((1073741821-e+t/10)/n|0)+1)*n}i(_e,"hg");function lt(e,t){if(e&&e.defaultProps){t=T({},t),e=e.defaultProps;for(var n in e)t[n]===void 0&&(t[n]=e[n])}return t}i(lt,"ig");var At={current:null},ft=null,ze=null,yt=null;function Wr(){yt=ze=ft=null}i(Wr,"ng");function zo(e){var t=At.current;We(At),e.type._context._currentValue=t}i(zo,"og");function _a(e,t){for(;e!==null;){var n=e.alternate;if(e.childExpirationTime<t)e.childExpirationTime=t,n!==null&&n.childExpirationTime<t&&(n.childExpirationTime=t);else if(n!==null&&n.childExpirationTime<t)n.childExpirationTime=t;else break;e=e.return}}i(_a,"pg");function Zr(e,t){ft=e,yt=ze=null,e=e.dependencies,e!==null&&e.firstContext!==null&&(e.expirationTime>=t&&(Jt=!0),e.firstContext=null)}i(Zr,"qg");function It(e,t){if(yt!==e&&t!==!1&&t!==0)if((typeof t!="number"||t===1073741823)&&(yt=e,t=1073741823),t={context:e,observedBits:t,next:null},ze===null){if(ft===null)throw Error(p(308));ze=t,ft.dependencies={expirationTime:0,firstContext:t,responders:null}}else ze=ze.next=t;return e._currentValue}i(It,"sg");var Rn=!1;function Os(e){e.updateQueue={baseState:e.memoizedState,baseQueue:null,shared:{pending:null},effects:null}}i(Os,"ug");function Ds(e,t){e=e.updateQueue,t.updateQueue===e&&(t.updateQueue={baseState:e.baseState,baseQueue:e.baseQueue,shared:e.shared,effects:e.effects})}i(Ds,"vg");function On(e,t){return e={expirationTime:e,suspenseConfig:t,tag:0,payload:null,callback:null,next:null},e.next=e}i(On,"wg");function Dn(e,t){if(e=e.updateQueue,e!==null){e=e.shared;var n=e.pending;n===null?t.next=t:(t.next=n.next,n.next=t),e.pending=t}}i(Dn,"xg");function La(e,t){var n=e.alternate;n!==null&&Ds(n,e),e=e.updateQueue,n=e.baseQueue,n===null?(e.baseQueue=t.next=t,t.next=t):(t.next=n.next,n.next=t)}i(La,"yg");function Bo(e,t,n,r){var s=e.updateQueue;Rn=!1;var d=s.baseQueue,m=s.shared.pending;if(m!==null){if(d!==null){var v=d.next;d.next=m.next,m.next=v}d=m,s.shared.pending=null,v=e.alternate,v!==null&&(v=v.updateQueue,v!==null&&(v.baseQueue=m))}if(d!==null){v=d.next;var L=s.baseState,S=0,te=null,ie=null,Ie=null;if(v!==null){var je=v;do{if(m=je.expirationTime,m<r){var Ft={expirationTime:je.expirationTime,suspenseConfig:je.suspenseConfig,tag:je.tag,payload:je.payload,callback:je.callback,next:null};Ie===null?(ie=Ie=Ft,te=L):Ie=Ie.next=Ft,m>S&&(S=m)}else{Ie!==null&&(Ie=Ie.next={expirationTime:1073741823,suspenseConfig:je.suspenseConfig,tag:je.tag,payload:je.payload,callback:je.callback,next:null}),Eu(m,je.suspenseConfig);e:{var vt=e,k=je;switch(m=t,Ft=n,k.tag){case 1:if(vt=k.payload,typeof vt=="function"){L=vt.call(Ft,L,m);break e}L=vt;break e;case 3:vt.effectTag=vt.effectTag&-4097|64;case 0:if(vt=k.payload,m=typeof vt=="function"?vt.call(Ft,L,m):vt,m==null)break e;L=T({},L,m);break e;case 2:Rn=!0}}je.callback!==null&&(e.effectTag|=32,m=s.effects,m===null?s.effects=[je]:m.push(je))}if(je=je.next,je===null||je===v){if(m=s.shared.pending,m===null)break;je=d.next=m.next,m.next=v,s.baseQueue=d=m,s.shared.pending=null}}while(!0)}Ie===null?te=L:Ie.next=ie,s.baseState=te,s.baseQueue=Ie,Nl(S),e.expirationTime=S,e.memoizedState=L}}i(Bo,"zg");function Sa(e,t,n){if(e=t.effects,t.effects=null,e!==null)for(t=0;t<e.length;t++){var r=e[t],s=r.callback;if(s!==null){if(r.callback=null,r=s,s=n,typeof r!="function")throw Error(p(191,r));r.call(s)}}}i(Sa,"Cg");var jo=kt.ReactCurrentBatchConfig,Ta=new V.Component().refs;function ol(e,t,n,r){t=e.memoizedState,n=n(r,t),n=n==null?t:T({},t,n),e.memoizedState=n,e.expirationTime===0&&(e.updateQueue.baseState=n)}i(ol,"Fg");var il={isMounted:i(function(e){return(e=e._reactInternalFiber)?on(e)===e:!1},"isMounted"),enqueueSetState:i(function(e,t,n){e=e._reactInternalFiber;var r=tn(),s=jo.suspense;r=vr(r,e,s),s=On(r,s),s.payload=t,n!=null&&(s.callback=n),Dn(e,s),Fn(e,r)},"enqueueSetState"),enqueueReplaceState:i(function(e,t,n){e=e._reactInternalFiber;var r=tn(),s=jo.suspense;r=vr(r,e,s),s=On(r,s),s.tag=1,s.payload=t,n!=null&&(s.callback=n),Dn(e,s),Fn(e,r)},"enqueueReplaceState"),enqueueForceUpdate:i(function(e,t){e=e._reactInternalFiber;var n=tn(),r=jo.suspense;n=vr(n,e,r),r=On(n,r),r.tag=2,t!=null&&(r.callback=t),Dn(e,r),Fn(e,n)},"enqueueForceUpdate")};function Ma(e,t,n,r,s,d,m){return e=e.stateNode,typeof e.shouldComponentUpdate=="function"?e.shouldComponentUpdate(r,d,m):t.prototype&&t.prototype.isPureReactComponent?!sr(n,r)||!sr(s,d):!0}i(Ma,"Kg");function Na(e,t,n){var r=!1,s=Kt,d=t.contextType;return typeof d=="object"&&d!==null?d=It(d):(s=dt(t)?jt:it.current,r=t.contextTypes,d=(r=r!=null)?Yt(e,s):Kt),t=new t(n,d),e.memoizedState=t.state!==null&&t.state!==void 0?t.state:null,t.updater=il,e.stateNode=t,t._reactInternalFiber=e,r&&(e=e.stateNode,e.__reactInternalMemoizedUnmaskedChildContext=s,e.__reactInternalMemoizedMaskedChildContext=d),t}i(Na,"Lg");function Pa(e,t,n,r){e=t.state,typeof t.componentWillReceiveProps=="function"&&t.componentWillReceiveProps(n,r),typeof t.UNSAFE_componentWillReceiveProps=="function"&&t.UNSAFE_componentWillReceiveProps(n,r),t.state!==e&&il.enqueueReplaceState(t,t.state,null)}i(Pa,"Mg");function As(e,t,n,r){var s=e.stateNode;s.props=n,s.state=e.memoizedState,s.refs=Ta,Os(e);var d=t.contextType;typeof d=="object"&&d!==null?s.context=It(d):(d=dt(t)?jt:it.current,s.context=Yt(e,d)),Bo(e,n,s,r),s.state=e.memoizedState,d=t.getDerivedStateFromProps,typeof d=="function"&&(ol(e,t,d,n),s.state=e.memoizedState),typeof t.getDerivedStateFromProps=="function"||typeof s.getSnapshotBeforeUpdate=="function"||typeof s.UNSAFE_componentWillMount!="function"&&typeof s.componentWillMount!="function"||(t=s.state,typeof s.componentWillMount=="function"&&s.componentWillMount(),typeof s.UNSAFE_componentWillMount=="function"&&s.UNSAFE_componentWillMount(),t!==s.state&&il.enqueueReplaceState(s,s.state,null),Bo(e,n,s,r),s.state=e.memoizedState),typeof s.componentDidMount=="function"&&(e.effectTag|=4)}i(As,"Ng");var ll=Array.isArray;function Uo(e,t,n){if(e=n.ref,e!==null&&typeof e!="function"&&typeof e!="object"){if(n._owner){if(n=n._owner,n){if(n.tag!==1)throw Error(p(309));var r=n.stateNode}if(!r)throw Error(p(147,e));var s=""+e;return t!==null&&t.ref!==null&&typeof t.ref=="function"&&t.ref._stringRef===s?t.ref:(t=i(function(d){var m=r.refs;m===Ta&&(m=r.refs={}),d===null?delete m[s]:m[s]=d},"b"),t._stringRef=s,t)}if(typeof e!="string")throw Error(p(284));if(!n._owner)throw Error(p(290,e))}return e}i(Uo,"Pg");function sl(e,t){if(e.type!=="textarea")throw Error(p(31,Object.prototype.toString.call(t)==="[object Object]"?"object with keys {"+Object.keys(t).join(", ")+"}":t,""))}i(sl,"Qg");function Ra(e){function t(k,x){if(e){var M=k.lastEffect;M!==null?(M.nextEffect=x,k.lastEffect=x):k.firstEffect=k.lastEffect=x,x.nextEffect=null,x.effectTag=8}}i(t,"b");function n(k,x){if(!e)return null;for(;x!==null;)t(k,x),x=x.sibling;return null}i(n,"c");function r(k,x){for(k=new Map;x!==null;)x.key!==null?k.set(x.key,x):k.set(x.index,x),x=x.sibling;return k}i(r,"d");function s(k,x){return k=wr(k,x),k.index=0,k.sibling=null,k}i(s,"e");function d(k,x,M){return k.index=M,e?(M=k.alternate,M!==null?(M=M.index,M<x?(k.effectTag=2,x):M):(k.effectTag=2,x)):x}i(d,"f");function m(k){return e&&k.alternate===null&&(k.effectTag=2),k}i(m,"g");function v(k,x,M,U){return x===null||x.tag!==6?(x=va(M,k.mode,U),x.return=k,x):(x=s(x,M),x.return=k,x)}i(v,"h");function L(k,x,M,U){return x!==null&&x.elementType===M.type?(U=s(x,M.props),U.ref=Uo(k,x,M),U.return=k,U):(U=Pl(M.type,M.key,M.props,null,k.mode,U),U.ref=Uo(k,x,M),U.return=k,U)}i(L,"k");function S(k,x,M,U){return x===null||x.tag!==4||x.stateNode.containerInfo!==M.containerInfo||x.stateNode.implementation!==M.implementation?(x=ga(M,k.mode,U),x.return=k,x):(x=s(x,M.children||[]),x.return=k,x)}i(S,"l");function te(k,x,M,U,ee){return x===null||x.tag!==7?(x=Vn(M,k.mode,U,ee),x.return=k,x):(x=s(x,M),x.return=k,x)}i(te,"m");function ie(k,x,M){if(typeof x=="string"||typeof x=="number")return x=va(""+x,k.mode,M),x.return=k,x;if(typeof x=="object"&&x!==null){switch(x.$$typeof){case Ce:return M=Pl(x.type,x.key,x.props,null,k.mode,M),M.ref=Uo(k,null,x),M.return=k,M;case zn:return x=ga(x,k.mode,M),x.return=k,x}if(ll(x)||jn(x))return x=Vn(x,k.mode,M,null),x.return=k,x;sl(k,x)}return null}i(ie,"p");function Ie(k,x,M,U){var ee=x!==null?x.key:null;if(typeof M=="string"||typeof M=="number")return ee!==null?null:v(k,x,""+M,U);if(typeof M=="object"&&M!==null){switch(M.$$typeof){case Ce:return M.key===ee?M.type===Wt?te(k,x,M.props.children,U,ee):L(k,x,M,U):null;case zn:return M.key===ee?S(k,x,M,U):null}if(ll(M)||jn(M))return ee!==null?null:te(k,x,M,U,null);sl(k,M)}return null}i(Ie,"x");function je(k,x,M,U,ee){if(typeof U=="string"||typeof U=="number")return k=k.get(M)||null,v(x,k,""+U,ee);if(typeof U=="object"&&U!==null){switch(U.$$typeof){case Ce:return k=k.get(U.key===null?M:U.key)||null,U.type===Wt?te(x,k,U.props.children,ee,U.key):L(x,k,U,ee);case zn:return k=k.get(U.key===null?M:U.key)||null,S(x,k,U,ee)}if(ll(U)||jn(U))return k=k.get(M)||null,te(x,k,U,ee,null);sl(x,U)}return null}i(je,"z");function Ft(k,x,M,U){for(var ee=null,le=null,ye=x,Fe=x=0,Je=null;ye!==null&&Fe<M.length;Fe++){ye.index>Fe?(Je=ye,ye=null):Je=ye.sibling;var Re=Ie(k,ye,M[Fe],U);if(Re===null){ye===null&&(ye=Je);break}e&&ye&&Re.alternate===null&&t(k,ye),x=d(Re,x,Fe),le===null?ee=Re:le.sibling=Re,le=Re,ye=Je}if(Fe===M.length)return n(k,ye),ee;if(ye===null){for(;Fe<M.length;Fe++)ye=ie(k,M[Fe],U),ye!==null&&(x=d(ye,x,Fe),le===null?ee=ye:le.sibling=ye,le=ye);return ee}for(ye=r(k,ye);Fe<M.length;Fe++)Je=je(ye,k,Fe,M[Fe],U),Je!==null&&(e&&Je.alternate!==null&&ye.delete(Je.key===null?Fe:Je.key),x=d(Je,x,Fe),le===null?ee=Je:le.sibling=Je,le=Je);return e&&ye.forEach(function($n){return t(k,$n)}),ee}i(Ft,"ca");function vt(k,x,M,U){var ee=jn(M);if(typeof ee!="function")throw Error(p(150));if(M=ee.call(M),M==null)throw Error(p(151));for(var le=ee=null,ye=x,Fe=x=0,Je=null,Re=M.next();ye!==null&&!Re.done;Fe++,Re=M.next()){ye.index>Fe?(Je=ye,ye=null):Je=ye.sibling;var $n=Ie(k,ye,Re.value,U);if($n===null){ye===null&&(ye=Je);break}e&&ye&&$n.alternate===null&&t(k,ye),x=d($n,x,Fe),le===null?ee=$n:le.sibling=$n,le=$n,ye=Je}if(Re.done)return n(k,ye),ee;if(ye===null){for(;!Re.done;Fe++,Re=M.next())Re=ie(k,Re.value,U),Re!==null&&(x=d(Re,x,Fe),le===null?ee=Re:le.sibling=Re,le=Re);return ee}for(ye=r(k,ye);!Re.done;Fe++,Re=M.next())Re=je(ye,k,Fe,Re.value,U),Re!==null&&(e&&Re.alternate!==null&&ye.delete(Re.key===null?Fe:Re.key),x=d(Re,x,Fe),le===null?ee=Re:le.sibling=Re,le=Re);return e&&ye.forEach(function(o1){return t(k,o1)}),ee}return i(vt,"D"),function(k,x,M,U){var ee=typeof M=="object"&&M!==null&&M.type===Wt&&M.key===null;ee&&(M=M.props.children);var le=typeof M=="object"&&M!==null;if(le)switch(M.$$typeof){case Ce:e:{for(le=M.key,ee=x;ee!==null;){if(ee.key===le){switch(ee.tag){case 7:if(M.type===Wt){n(k,ee.sibling),x=s(ee,M.props.children),x.return=k,k=x;break e}break;default:if(ee.elementType===M.type){n(k,ee.sibling),x=s(ee,M.props),x.ref=Uo(k,ee,M),x.return=k,k=x;break e}}n(k,ee);break}else t(k,ee);ee=ee.sibling}M.type===Wt?(x=Vn(M.props.children,k.mode,U,M.key),x.return=k,k=x):(U=Pl(M.type,M.key,M.props,null,k.mode,U),U.ref=Uo(k,x,M),U.return=k,k=U)}return m(k);case zn:e:{for(ee=M.key;x!==null;){if(x.key===ee)if(x.tag===4&&x.stateNode.containerInfo===M.containerInfo&&x.stateNode.implementation===M.implementation){n(k,x.sibling),x=s(x,M.children||[]),x.return=k,k=x;break e}else{n(k,x);break}else t(k,x);x=x.sibling}x=ga(M,k.mode,U),x.return=k,k=x}return m(k)}if(typeof M=="string"||typeof M=="number")return M=""+M,x!==null&&x.tag===6?(n(k,x.sibling),x=s(x,M),x.return=k,k=x):(n(k,x),x=va(M,k.mode,U),x.return=k,k=x),m(k);if(ll(M))return Ft(k,x,M,U);if(jn(M))return vt(k,x,M,U);if(le&&sl(k,M),typeof M=="undefined"&&!ee)switch(k.tag){case 1:case 0:throw k=k.type,Error(p(152,k.displayName||k.name||"Component"))}return n(k,x)}}i(Ra,"Rg");var qr=Ra(!0),Is=Ra(!1),Wo={},Xt={current:Wo},Zo={current:Wo},qo={current:Wo};function dr(e){if(e===Wo)throw Error(p(174));return e}i(dr,"ch");function Hs(e,t){switch(Ge(qo,t),Ge(Zo,e),Ge(Xt,Wo),e=t.nodeType,e){case 9:case 11:t=(t=t.documentElement)?t.namespaceURI:oo(null,"");break;default:e=e===8?t.parentNode:t,t=e.namespaceURI||null,e=e.tagName,t=oo(t,e)}We(Xt),Ge(Xt,t)}i(Hs,"dh");function Qr(){We(Xt),We(Zo),We(qo)}i(Qr,"eh");function Oa(e){dr(qo.current);var t=dr(Xt.current),n=oo(t,e.type);t!==n&&(Ge(Zo,e),Ge(Xt,n))}i(Oa,"fh");function Fs(e){Zo.current===e&&(We(Xt),We(Zo))}i(Fs,"gh");var et={current:0};function al(e){for(var t=e;t!==null;){if(t.tag===13){var n=t.memoizedState;if(n!==null&&(n=n.dehydrated,n===null||n.data===ko||n.data===bo))return t}else if(t.tag===19&&t.memoizedProps.revealOrder!==void 0){if(t.effectTag&64)return t}else if(t.child!==null){t.child.return=t,t=t.child;continue}if(t===e)break;for(;t.sibling===null;){if(t.return===null||t.return===e)return null;t=t.return}t.sibling.return=t.return,t=t.sibling}return null}i(al,"hh");function Vs(e,t){return{responder:e,props:t}}i(Vs,"ih");var ul=kt.ReactCurrentDispatcher,Ht=kt.ReactCurrentBatchConfig,An=0,st=null,Ct=null,wt=null,cl=!1;function Tt(){throw Error(p(321))}i(Tt,"Q");function $s(e,t){if(t===null)return!1;for(var n=0;n<t.length&&n<e.length;n++)if(!gn(e[n],t[n]))return!1;return!0}i($s,"nh");function zs(e,t,n,r,s,d){if(An=d,st=t,t.memoizedState=null,t.updateQueue=null,t.expirationTime=0,ul.current=e===null||e.memoizedState===null?Pu:Ru,e=n(r,s),t.expirationTime===An){d=0;do{if(t.expirationTime=0,!(25>d))throw Error(p(301));d+=1,wt=Ct=null,t.updateQueue=null,ul.current=Ou,e=n(r,s)}while(t.expirationTime===An)}if(ul.current=hl,t=Ct!==null&&Ct.next!==null,An=0,wt=Ct=st=null,cl=!1,t)throw Error(p(300));return e}i(zs,"oh");function Kr(){var e={memoizedState:null,baseState:null,baseQueue:null,queue:null,next:null};return wt===null?st.memoizedState=wt=e:wt=wt.next=e,wt}i(Kr,"th");function Yr(){if(Ct===null){var e=st.alternate;e=e!==null?e.memoizedState:null}else e=Ct.next;var t=wt===null?st.memoizedState:wt.next;if(t!==null)wt=t,Ct=e;else{if(e===null)throw Error(p(310));Ct=e,e={memoizedState:Ct.memoizedState,baseState:Ct.baseState,baseQueue:Ct.baseQueue,queue:Ct.queue,next:null},wt===null?st.memoizedState=wt=e:wt=wt.next=e}return wt}i(Yr,"uh");function fr(e,t){return typeof t=="function"?t(e):t}i(fr,"vh");function dl(e){var t=Yr(),n=t.queue;if(n===null)throw Error(p(311));n.lastRenderedReducer=e;var r=Ct,s=r.baseQueue,d=n.pending;if(d!==null){if(s!==null){var m=s.next;s.next=d.next,d.next=m}r.baseQueue=s=d,n.pending=null}if(s!==null){s=s.next,r=r.baseState;var v=m=d=null,L=s;do{var S=L.expirationTime;if(S<An){var te={expirationTime:L.expirationTime,suspenseConfig:L.suspenseConfig,action:L.action,eagerReducer:L.eagerReducer,eagerState:L.eagerState,next:null};v===null?(m=v=te,d=r):v=v.next=te,S>st.expirationTime&&(st.expirationTime=S,Nl(S))}else v!==null&&(v=v.next={expirationTime:1073741823,suspenseConfig:L.suspenseConfig,action:L.action,eagerReducer:L.eagerReducer,eagerState:L.eagerState,next:null}),Eu(S,L.suspenseConfig),r=L.eagerReducer===e?L.eagerState:e(r,L.action);L=L.next}while(L!==null&&L!==s);v===null?d=r:v.next=m,gn(r,t.memoizedState)||(Jt=!0),t.memoizedState=r,t.baseState=d,t.baseQueue=v,n.lastRenderedState=r}return[t.memoizedState,n.dispatch]}i(dl,"wh");function fl(e){var t=Yr(),n=t.queue;if(n===null)throw Error(p(311));n.lastRenderedReducer=e;var r=n.dispatch,s=n.pending,d=t.memoizedState;if(s!==null){n.pending=null;var m=s=s.next;do d=e(d,m.action),m=m.next;while(m!==s);gn(d,t.memoizedState)||(Jt=!0),t.memoizedState=d,t.baseQueue===null&&(t.baseState=d),n.lastRenderedState=d}return[d,r]}i(fl,"xh");function Bs(e){var t=Kr();return typeof e=="function"&&(e=e()),t.memoizedState=t.baseState=e,e=t.queue={pending:null,dispatch:null,lastRenderedReducer:fr,lastRenderedState:e},e=e.dispatch=za.bind(null,st,e),[t.memoizedState,e]}i(Bs,"yh");function js(e,t,n,r){return e={tag:e,create:t,destroy:n,deps:r,next:null},t=st.updateQueue,t===null?(t={lastEffect:null},st.updateQueue=t,t.lastEffect=e.next=e):(n=t.lastEffect,n===null?t.lastEffect=e.next=e:(r=n.next,n.next=e,e.next=r,t.lastEffect=e)),e}i(js,"Ah");function Da(){return Yr().memoizedState}i(Da,"Bh");function Us(e,t,n,r){var s=Kr();st.effectTag|=e,s.memoizedState=js(1|t,n,void 0,r===void 0?null:r)}i(Us,"Ch");function Ws(e,t,n,r){var s=Yr();r=r===void 0?null:r;var d=void 0;if(Ct!==null){var m=Ct.memoizedState;if(d=m.destroy,r!==null&&$s(r,m.deps)){js(t,n,d,r);return}}st.effectTag|=e,s.memoizedState=js(1|t,n,d,r)}i(Ws,"Dh");function Aa(e,t){return Us(516,4,e,t)}i(Aa,"Eh");function ml(e,t){return Ws(516,4,e,t)}i(ml,"Fh");function Ia(e,t){return Ws(4,2,e,t)}i(Ia,"Gh");function Ha(e,t){if(typeof t=="function")return e=e(),t(e),function(){t(null)};if(t!=null)return e=e(),t.current=e,function(){t.current=null}}i(Ha,"Hh");function Fa(e,t,n){return n=n!=null?n.concat([e]):null,Ws(4,2,Ha.bind(null,t,e),n)}i(Fa,"Ih");function Zs(){}i(Zs,"Jh");function Va(e,t){return Kr().memoizedState=[e,t===void 0?null:t],e}i(Va,"Kh");function pl(e,t){var n=Yr();t=t===void 0?null:t;var r=n.memoizedState;return r!==null&&t!==null&&$s(t,r[1])?r[0]:(n.memoizedState=[e,t],e)}i(pl,"Lh");function $a(e,t){var n=Yr();t=t===void 0?null:t;var r=n.memoizedState;return r!==null&&t!==null&&$s(t,r[1])?r[0]:(e=e(),n.memoizedState=[e,t],e)}i($a,"Mh");function qs(e,t,n){var r=Pe();Te(98>r?98:r,function(){e(!0)}),Te(97<r?97:r,function(){var s=Ht.suspense;Ht.suspense=t===void 0?null:t;try{e(!1),n()}finally{Ht.suspense=s}})}i(qs,"Nh");function za(e,t,n){var r=tn(),s=jo.suspense;r=vr(r,e,s),s={expirationTime:r,suspenseConfig:s,action:n,eagerReducer:null,eagerState:null,next:null};var d=t.pending;if(d===null?s.next=s:(s.next=d.next,d.next=s),t.pending=s,d=e.alternate,e===st||d!==null&&d===st)cl=!0,s.expirationTime=An,st.expirationTime=An;else{if(e.expirationTime===0&&(d===null||d.expirationTime===0)&&(d=t.lastRenderedReducer,d!==null))try{var m=t.lastRenderedState,v=d(m,n);if(s.eagerReducer=d,s.eagerState=v,gn(v,m))return}catch{}finally{}Fn(e,r)}}i(za,"zh");var hl={readContext:It,useCallback:Tt,useContext:Tt,useEffect:Tt,useImperativeHandle:Tt,useLayoutEffect:Tt,useMemo:Tt,useReducer:Tt,useRef:Tt,useState:Tt,useDebugValue:Tt,useResponder:Tt,useDeferredValue:Tt,useTransition:Tt},Pu={readContext:It,useCallback:Va,useContext:It,useEffect:Aa,useImperativeHandle:i(function(e,t,n){return n=n!=null?n.concat([e]):null,Us(4,2,Ha.bind(null,t,e),n)},"useImperativeHandle"),useLayoutEffect:i(function(e,t){return Us(4,2,e,t)},"useLayoutEffect"),useMemo:i(function(e,t){var n=Kr();return t=t===void 0?null:t,e=e(),n.memoizedState=[e,t],e},"useMemo"),useReducer:i(function(e,t,n){var r=Kr();return t=n!==void 0?n(t):t,r.memoizedState=r.baseState=t,e=r.queue={pending:null,dispatch:null,lastRenderedReducer:e,lastRenderedState:t},e=e.dispatch=za.bind(null,st,e),[r.memoizedState,e]},"useReducer"),useRef:i(function(e){var t=Kr();return e={current:e},t.memoizedState=e},"useRef"),useState:Bs,useDebugValue:Zs,useResponder:Vs,useDeferredValue:i(function(e,t){var n=Bs(e),r=n[0],s=n[1];return Aa(function(){var d=Ht.suspense;Ht.suspense=t===void 0?null:t;try{s(e)}finally{Ht.suspense=d}},[e,t]),r},"useDeferredValue"),useTransition:i(function(e){var t=Bs(!1),n=t[0];return t=t[1],[Va(qs.bind(null,t,e),[t,e]),n]},"useTransition")},Ru={readContext:It,useCallback:pl,useContext:It,useEffect:ml,useImperativeHandle:Fa,useLayoutEffect:Ia,useMemo:$a,useReducer:dl,useRef:Da,useState:i(function(){return dl(fr)},"useState"),useDebugValue:Zs,useResponder:Vs,useDeferredValue:i(function(e,t){var n=dl(fr),r=n[0],s=n[1];return ml(function(){var d=Ht.suspense;Ht.suspense=t===void 0?null:t;try{s(e)}finally{Ht.suspense=d}},[e,t]),r},"useDeferredValue"),useTransition:i(function(e){var t=dl(fr),n=t[0];return t=t[1],[pl(qs.bind(null,t,e),[t,e]),n]},"useTransition")},Ou={readContext:It,useCallback:pl,useContext:It,useEffect:ml,useImperativeHandle:Fa,useLayoutEffect:Ia,useMemo:$a,useReducer:fl,useRef:Da,useState:i(function(){return fl(fr)},"useState"),useDebugValue:Zs,useResponder:Vs,useDeferredValue:i(function(e,t){var n=fl(fr),r=n[0],s=n[1];return ml(function(){var d=Ht.suspense;Ht.suspense=t===void 0?null:t;try{s(e)}finally{Ht.suspense=d}},[e,t]),r},"useDeferredValue"),useTransition:i(function(e){var t=fl(fr),n=t[0];return t=t[1],[pl(qs.bind(null,t,e),[t,e]),n]},"useTransition")},yn=null,In=null,mr=!1;function Ba(e,t){var n=nn(5,null,null,0);n.elementType="DELETED",n.type="DELETED",n.stateNode=t,n.return=e,n.effectTag=8,e.lastEffect!==null?(e.lastEffect.nextEffect=n,e.lastEffect=n):e.firstEffect=e.lastEffect=n}i(Ba,"Rh");function ja(e,t){switch(e.tag){case 5:var n=e.type;return t=t.nodeType!==1||n.toLowerCase()!==t.nodeName.toLowerCase()?null:t,t!==null?(e.stateNode=t,!0):!1;case 6:return t=e.pendingProps===""||t.nodeType!==3?null:t,t!==null?(e.stateNode=t,!0):!1;case 13:return!1;default:return!1}}i(ja,"Th");function Qs(e){if(mr){var t=In;if(t){var n=t;if(!ja(e,t)){if(t=pn(n.nextSibling),!t||!ja(e,t)){e.effectTag=e.effectTag&-1025|2,mr=!1,yn=e;return}Ba(yn,n)}yn=e,In=pn(t.firstChild)}else e.effectTag=e.effectTag&-1025|2,mr=!1,yn=e}}i(Qs,"Uh");function Ua(e){for(e=e.return;e!==null&&e.tag!==5&&e.tag!==3&&e.tag!==13;)e=e.return;yn=e}i(Ua,"Vh");function vl(e){if(e!==yn)return!1;if(!mr)return Ua(e),mr=!0,!1;var t=e.type;if(e.tag!==5||t!=="head"&&t!=="body"&&!So(t,e.memoizedProps))for(t=In;t;)Ba(e,t),t=pn(t.nextSibling);if(Ua(e),e.tag===13){if(e=e.memoizedState,e=e!==null?e.dehydrated:null,!e)throw Error(p(317));e:{for(e=e.nextSibling,t=0;e;){if(e.nodeType===8){var n=e.data;if(n===Di){if(t===0){In=pn(e.nextSibling);break e}t--}else n!==Nr&&n!==bo&&n!==ko||t++}e=e.nextSibling}In=null}}else In=yn?pn(e.stateNode.nextSibling):null;return!0}i(vl,"Wh");function Ks(){In=yn=null,mr=!1}i(Ks,"Xh");var Du=kt.ReactCurrentOwner,Jt=!1;function Mt(e,t,n,r){t.child=e===null?Is(t,null,n,r):qr(t,e.child,n,r)}i(Mt,"R");function Wa(e,t,n,r,s){n=n.render;var d=t.ref;return Zr(t,s),r=zs(e,t,n,r,d,s),e!==null&&!Jt?(t.updateQueue=e.updateQueue,t.effectTag&=-517,e.expirationTime<=s&&(e.expirationTime=0),Cn(e,t,s)):(t.effectTag|=1,Mt(e,t,r,s),t.child)}i(Wa,"Zh");function Za(e,t,n,r,s,d){if(e===null){var m=n.type;return typeof m=="function"&&!ha(m)&&m.defaultProps===void 0&&n.compare===null&&n.defaultProps===void 0?(t.tag=15,t.type=m,qa(e,t,m,r,s,d)):(e=Pl(n.type,null,r,null,t.mode,d),e.ref=t.ref,e.return=t,t.child=e)}return m=e.child,s<d&&(s=m.memoizedProps,n=n.compare,n=n!==null?n:sr,n(s,r)&&e.ref===t.ref)?Cn(e,t,d):(t.effectTag|=1,e=wr(m,r),e.ref=t.ref,e.return=t,t.child=e)}i(Za,"ai");function qa(e,t,n,r,s,d){return e!==null&&sr(e.memoizedProps,r)&&e.ref===t.ref&&(Jt=!1,s<d)?(t.expirationTime=e.expirationTime,Cn(e,t,d)):Ys(e,t,n,r,d)}i(qa,"ci");function Qa(e,t){var n=t.ref;(e===null&&n!==null||e!==null&&e.ref!==n)&&(t.effectTag|=128)}i(Qa,"ei");function Ys(e,t,n,r,s){var d=dt(n)?jt:it.current;return d=Yt(t,d),Zr(t,s),n=zs(e,t,n,r,d,s),e!==null&&!Jt?(t.updateQueue=e.updateQueue,t.effectTag&=-517,e.expirationTime<=s&&(e.expirationTime=0),Cn(e,t,s)):(t.effectTag|=1,Mt(e,t,n,s),t.child)}i(Ys,"di");function Ka(e,t,n,r,s){if(dt(n)){var d=!0;Pn(t)}else d=!1;if(Zr(t,s),t.stateNode===null)e!==null&&(e.alternate=null,t.alternate=null,t.effectTag|=2),Na(t,n,r),As(t,n,r,s),r=!0;else if(e===null){var m=t.stateNode,v=t.memoizedProps;m.props=v;var L=m.context,S=n.contextType;typeof S=="object"&&S!==null?S=It(S):(S=dt(n)?jt:it.current,S=Yt(t,S));var te=n.getDerivedStateFromProps,ie=typeof te=="function"||typeof m.getSnapshotBeforeUpdate=="function";ie||typeof m.UNSAFE_componentWillReceiveProps!="function"&&typeof m.componentWillReceiveProps!="function"||(v!==r||L!==S)&&Pa(t,m,r,S),Rn=!1;var Ie=t.memoizedState;m.state=Ie,Bo(t,r,m,s),L=t.memoizedState,v!==r||Ie!==L||nt.current||Rn?(typeof te=="function"&&(ol(t,n,te,r),L=t.memoizedState),(v=Rn||Ma(t,n,v,r,Ie,L,S))?(ie||typeof m.UNSAFE_componentWillMount!="function"&&typeof m.componentWillMount!="function"||(typeof m.componentWillMount=="function"&&m.componentWillMount(),typeof m.UNSAFE_componentWillMount=="function"&&m.UNSAFE_componentWillMount()),typeof m.componentDidMount=="function"&&(t.effectTag|=4)):(typeof m.componentDidMount=="function"&&(t.effectTag|=4),t.memoizedProps=r,t.memoizedState=L),m.props=r,m.state=L,m.context=S,r=v):(typeof m.componentDidMount=="function"&&(t.effectTag|=4),r=!1)}else m=t.stateNode,Ds(e,t),v=t.memoizedProps,m.props=t.type===t.elementType?v:lt(t.type,v),L=m.context,S=n.contextType,typeof S=="object"&&S!==null?S=It(S):(S=dt(n)?jt:it.current,S=Yt(t,S)),te=n.getDerivedStateFromProps,(ie=typeof te=="function"||typeof m.getSnapshotBeforeUpdate=="function")||typeof m.UNSAFE_componentWillReceiveProps!="function"&&typeof m.componentWillReceiveProps!="function"||(v!==r||L!==S)&&Pa(t,m,r,S),Rn=!1,L=t.memoizedState,m.state=L,Bo(t,r,m,s),Ie=t.memoizedState,v!==r||L!==Ie||nt.current||Rn?(typeof te=="function"&&(ol(t,n,te,r),Ie=t.memoizedState),(te=Rn||Ma(t,n,v,r,L,Ie,S))?(ie||typeof m.UNSAFE_componentWillUpdate!="function"&&typeof m.componentWillUpdate!="function"||(typeof m.componentWillUpdate=="function"&&m.componentWillUpdate(r,Ie,S),typeof m.UNSAFE_componentWillUpdate=="function"&&m.UNSAFE_componentWillUpdate(r,Ie,S)),typeof m.componentDidUpdate=="function"&&(t.effectTag|=4),typeof m.getSnapshotBeforeUpdate=="function"&&(t.effectTag|=256)):(typeof m.componentDidUpdate!="function"||v===e.memoizedProps&&L===e.memoizedState||(t.effectTag|=4),typeof m.getSnapshotBeforeUpdate!="function"||v===e.memoizedProps&&L===e.memoizedState||(t.effectTag|=256),t.memoizedProps=r,t.memoizedState=Ie),m.props=r,m.state=Ie,m.context=S,r=te):(typeof m.componentDidUpdate!="function"||v===e.memoizedProps&&L===e.memoizedState||(t.effectTag|=4),typeof m.getSnapshotBeforeUpdate!="function"||v===e.memoizedProps&&L===e.memoizedState||(t.effectTag|=256),r=!1);return Gs(e,t,n,r,d,s)}i(Ka,"fi");function Gs(e,t,n,r,s,d){Qa(e,t);var m=(t.effectTag&64)!==0;if(!r&&!m)return s&&Gt(t,n,!1),Cn(e,t,d);r=t.stateNode,Du.current=t;var v=m&&typeof n.getDerivedStateFromError!="function"?null:r.render();return t.effectTag|=1,e!==null&&m?(t.child=qr(t,e.child,null,d),t.child=qr(t,null,v,d)):Mt(e,t,v,d),t.memoizedState=r.state,s&&Gt(t,n,!0),t.child}i(Gs,"gi");function Ya(e){var t=e.stateNode;t.pendingContext?zr(e,t.pendingContext,t.pendingContext!==t.context):t.context&&zr(e,t.context,!1),Hs(e,t.containerInfo)}i(Ya,"hi");var Xs={dehydrated:null,retryTime:0};function Ga(e,t,n){var r=t.mode,s=t.pendingProps,d=et.current,m=!1,v;if((v=(t.effectTag&64)!==0)||(v=(d&2)!==0&&(e===null||e.memoizedState!==null)),v?(m=!0,t.effectTag&=-65):e!==null&&e.memoizedState===null||s.fallback===void 0||s.unstable_avoidThisFallback===!0||(d|=1),Ge(et,d&1),e===null){if(s.fallback!==void 0&&Qs(t),m){if(m=s.fallback,s=Vn(null,r,0,null),s.return=t,!(t.mode&2))for(e=t.memoizedState!==null?t.child.child:t.child,s.child=e;e!==null;)e.return=s,e=e.sibling;return n=Vn(m,r,n,null),n.return=t,s.sibling=n,t.memoizedState=Xs,t.child=s,n}return r=s.children,t.memoizedState=null,t.child=Is(t,null,r,n)}if(e.memoizedState!==null){if(e=e.child,r=e.sibling,m){if(s=s.fallback,n=wr(e,e.pendingProps),n.return=t,!(t.mode&2)&&(m=t.memoizedState!==null?t.child.child:t.child,m!==e.child))for(n.child=m;m!==null;)m.return=n,m=m.sibling;return r=wr(r,s),r.return=t,n.sibling=r,n.childExpirationTime=0,t.memoizedState=Xs,t.child=n,r}return n=qr(t,e.child,s.children,n),t.memoizedState=null,t.child=n}if(e=e.child,m){if(m=s.fallback,s=Vn(null,r,0,null),s.return=t,s.child=e,e!==null&&(e.return=s),!(t.mode&2))for(e=t.memoizedState!==null?t.child.child:t.child,s.child=e;e!==null;)e.return=s,e=e.sibling;return n=Vn(m,r,n,null),n.return=t,s.sibling=n,n.effectTag|=2,s.childExpirationTime=0,t.memoizedState=Xs,t.child=s,n}return t.memoizedState=null,t.child=qr(t,e,s.children,n)}i(Ga,"ji");function Xa(e,t){e.expirationTime<t&&(e.expirationTime=t);var n=e.alternate;n!==null&&n.expirationTime<t&&(n.expirationTime=t),_a(e.return,t)}i(Xa,"ki");function Js(e,t,n,r,s,d){var m=e.memoizedState;m===null?e.memoizedState={isBackwards:t,rendering:null,renderingStartTime:0,last:r,tail:n,tailExpiration:0,tailMode:s,lastEffect:d}:(m.isBackwards=t,m.rendering=null,m.renderingStartTime=0,m.last=r,m.tail=n,m.tailExpiration=0,m.tailMode=s,m.lastEffect=d)}i(Js,"li");function Ja(e,t,n){var r=t.pendingProps,s=r.revealOrder,d=r.tail;if(Mt(e,t,r.children,n),r=et.current,r&2)r=r&1|2,t.effectTag|=64;else{if(e!==null&&e.effectTag&64)e:for(e=t.child;e!==null;){if(e.tag===13)e.memoizedState!==null&&Xa(e,n);else if(e.tag===19)Xa(e,n);else if(e.child!==null){e.child.return=e,e=e.child;continue}if(e===t)break e;for(;e.sibling===null;){if(e.return===null||e.return===t)break e;e=e.return}e.sibling.return=e.return,e=e.sibling}r&=1}if(Ge(et,r),!(t.mode&2))t.memoizedState=null;else switch(s){case"forwards":for(n=t.child,s=null;n!==null;)e=n.alternate,e!==null&&al(e)===null&&(s=n),n=n.sibling;n=s,n===null?(s=t.child,t.child=null):(s=n.sibling,n.sibling=null),Js(t,!1,s,n,d,t.lastEffect);break;case"backwards":for(n=null,s=t.child,t.child=null;s!==null;){if(e=s.alternate,e!==null&&al(e)===null){t.child=s;break}e=s.sibling,s.sibling=n,n=s,s=e}Js(t,!0,n,null,d,t.lastEffect);break;case"together":Js(t,!1,null,null,void 0,t.lastEffect);break;default:t.memoizedState=null}return t.child}i(Ja,"mi");function Cn(e,t,n){e!==null&&(t.dependencies=e.dependencies);var r=t.expirationTime;if(r!==0&&Nl(r),t.childExpirationTime<n)return null;if(e!==null&&t.child!==e.child)throw Error(p(153));if(t.child!==null){for(e=t.child,n=wr(e,e.pendingProps),t.child=n,n.return=t;e.sibling!==null;)e=e.sibling,n=n.sibling=wr(e,e.pendingProps),n.return=t;n.sibling=null}return t.child}i(Cn,"$h");var eu,ea,tu,nu;eu=i(function(e,t){for(var n=t.child;n!==null;){if(n.tag===5||n.tag===6)e.appendChild(n.stateNode);else if(n.tag!==4&&n.child!==null){n.child.return=n,n=n.child;continue}if(n===t)break;for(;n.sibling===null;){if(n.return===null||n.return===t)return;n=n.return}n.sibling.return=n.return,n=n.sibling}},"ni"),ea=i(function(){},"oi"),tu=i(function(e,t,n,r,s){var d=e.memoizedProps;if(d!==r){var m=t.stateNode;switch(dr(Xt.current),e=null,n){case"input":d=si(m,d),r=si(m,r),e=[];break;case"option":d=ci(m,d),r=ci(m,r),e=[];break;case"select":d=T({},d,{value:void 0}),r=T({},r,{value:void 0}),e=[];break;case"textarea":d=di(m,d),r=di(m,r),e=[];break;default:typeof d.onClick!="function"&&typeof r.onClick=="function"&&(m.onclick=er)}Co(n,r);var v,L;n=null;for(v in d)if(!r.hasOwnProperty(v)&&d.hasOwnProperty(v)&&d[v]!=null)if(v==="style")for(L in m=d[v],m)m.hasOwnProperty(L)&&(n||(n={}),n[L]="");else v!=="dangerouslySetInnerHTML"&&v!=="children"&&v!=="suppressContentEditableWarning"&&v!=="suppressHydrationWarning"&&v!=="autoFocus"&&(I.hasOwnProperty(v)?e||(e=[]):(e=e||[]).push(v,null));for(v in r){var S=r[v];if(m=d!=null?d[v]:void 0,r.hasOwnProperty(v)&&S!==m&&(S!=null||m!=null))if(v==="style")if(m){for(L in m)!m.hasOwnProperty(L)||S&&S.hasOwnProperty(L)||(n||(n={}),n[L]="");for(L in S)S.hasOwnProperty(L)&&m[L]!==S[L]&&(n||(n={}),n[L]=S[L])}else n||(e||(e=[]),e.push(v,n)),n=S;else v==="dangerouslySetInnerHTML"?(S=S?S.__html:void 0,m=m?m.__html:void 0,S!=null&&m!==S&&(e=e||[]).push(v,S)):v==="children"?m===S||typeof S!="string"&&typeof S!="number"||(e=e||[]).push(v,""+S):v!=="suppressContentEditableWarning"&&v!=="suppressHydrationWarning"&&(I.hasOwnProperty(v)?(S!=null&&Dt(s,v),e||m===S||(e=[])):(e=e||[]).push(v,S))}n&&(e=e||[]).push("style",n),s=e,(t.updateQueue=s)&&(t.effectTag|=4)}},"pi"),nu=i(function(e,t,n,r){n!==r&&(t.effectTag|=4)},"qi");function gl(e,t){switch(e.tailMode){case"hidden":t=e.tail;for(var n=null;t!==null;)t.alternate!==null&&(n=t),t=t.sibling;n===null?e.tail=null:n.sibling=null;break;case"collapsed":n=e.tail;for(var r=null;n!==null;)n.alternate!==null&&(r=n),n=n.sibling;r===null?t||e.tail===null?e.tail=null:e.tail.sibling=null:r.sibling=null}}i(gl,"ri");function Au(e,t,n){var r=t.pendingProps;switch(t.tag){case 2:case 16:case 15:case 0:case 11:case 7:case 8:case 12:case 9:case 14:return null;case 1:return dt(t.type)&&Nn(),null;case 3:return Qr(),We(nt),We(it),n=t.stateNode,n.pendingContext&&(n.context=n.pendingContext,n.pendingContext=null),e!==null&&e.child!==null||!vl(t)||(t.effectTag|=4),ea(t),null;case 5:Fs(t),n=dr(qo.current);var s=t.type;if(e!==null&&t.stateNode!=null)tu(e,t,s,r,n),e.ref!==t.ref&&(t.effectTag|=128);else{if(!r){if(t.stateNode===null)throw Error(p(166));return null}if(e=dr(Xt.current),vl(t)){r=t.stateNode,s=t.type;var d=t.memoizedProps;switch(r[Zt]=t,r[Pr]=d,s){case"iframe":case"object":case"embed":Qe("load",r);break;case"video":case"audio":for(e=0;e<Kn.length;e++)Qe(Kn[e],r);break;case"source":Qe("error",r);break;case"img":case"image":case"link":Qe("error",r),Qe("load",r);break;case"form":Qe("reset",r),Qe("submit",r);break;case"details":Qe("toggle",r);break;case"input":Fl(r,d),Qe("invalid",r),Dt(n,"onChange");break;case"select":r._wrapperState={wasMultiple:!!d.multiple},Qe("invalid",r),Dt(n,"onChange");break;case"textarea":fi(r,d),Qe("invalid",r),Dt(n,"onChange")}Co(s,d),e=null;for(var m in d)if(d.hasOwnProperty(m)){var v=d[m];m==="children"?typeof v=="string"?r.textContent!==v&&(e=["children",v]):typeof v=="number"&&r.textContent!==""+v&&(e=["children",""+v]):I.hasOwnProperty(m)&&v!=null&&Dt(n,m)}switch(s){case"input":_t(r),$l(r,d,!0);break;case"textarea":_t(r),pi(r);break;case"select":case"option":break;default:typeof d.onClick=="function"&&(r.onclick=er)}n=e,t.updateQueue=n,n!==null&&(t.effectTag|=4)}else{switch(m=n.nodeType===9?n:n.ownerDocument,e===Ri&&(e=vi(s)),e===Ri?s==="script"?(e=m.createElement("div"),e.innerHTML="<script><\/script>",e=e.removeChild(e.firstChild)):typeof r.is=="string"?e=m.createElement(s,{is:r.is}):(e=m.createElement(s),s==="select"&&(m=e,r.multiple?m.multiple=!0:r.size&&(m.size=r.size))):e=m.createElementNS(e,s),e[Zt]=t,e[Pr]=r,eu(e,t,!1,!1),t.stateNode=e,m=wo(s,r),s){case"iframe":case"object":case"embed":Qe("load",e),v=r;break;case"video":case"audio":for(v=0;v<Kn.length;v++)Qe(Kn[v],e);v=r;break;case"source":Qe("error",e),v=r;break;case"img":case"image":case"link":Qe("error",e),Qe("load",e),v=r;break;case"form":Qe("reset",e),Qe("submit",e),v=r;break;case"details":Qe("toggle",e),v=r;break;case"input":Fl(e,r),v=si(e,r),Qe("invalid",e),Dt(n,"onChange");break;case"option":v=ci(e,r);break;case"select":e._wrapperState={wasMultiple:!!r.multiple},v=T({},r,{value:void 0}),Qe("invalid",e),Dt(n,"onChange");break;case"textarea":fi(e,r),v=di(e,r),Qe("invalid",e),Dt(n,"onChange");break;default:v=r}Co(s,v);var L=v;for(d in L)if(L.hasOwnProperty(d)){var S=L[d];d==="style"?yo(e,S):d==="dangerouslySetInnerHTML"?(S=S?S.__html:void 0,S!=null&&gi(e,S)):d==="children"?typeof S=="string"?(s!=="textarea"||S!=="")&&Zn(e,S):typeof S=="number"&&Zn(e,""+S):d!=="suppressContentEditableWarning"&&d!=="suppressHydrationWarning"&&d!=="autoFocus"&&(I.hasOwnProperty(d)?S!=null&&Dt(n,d):S!=null&&Jr(e,d,S,m))}switch(s){case"input":_t(e),$l(e,r,!1);break;case"textarea":_t(e),pi(e);break;case"option":r.value!=null&&e.setAttribute("value",""+Ot(r.value));break;case"select":e.multiple=!!r.multiple,n=r.value,n!=null?Wn(e,!!r.multiple,n,!1):r.defaultValue!=null&&Wn(e,!!r.multiple,r.defaultValue,!0);break;default:typeof v.onClick=="function"&&(e.onclick=er)}Ai(s,r)&&(t.effectTag|=4)}t.ref!==null&&(t.effectTag|=128)}return null;case 6:if(e&&t.stateNode!=null)nu(e,t,e.memoizedProps,r);else{if(typeof r!="string"&&t.stateNode===null)throw Error(p(166));n=dr(qo.current),dr(Xt.current),vl(t)?(n=t.stateNode,r=t.memoizedProps,n[Zt]=t,n.nodeValue!==r&&(t.effectTag|=4)):(n=(n.nodeType===9?n:n.ownerDocument).createTextNode(r),n[Zt]=t,t.stateNode=n)}return null;case 13:return We(et),r=t.memoizedState,t.effectTag&64?(t.expirationTime=n,t):(n=r!==null,r=!1,e===null?t.memoizedProps.fallback!==void 0&&vl(t):(s=e.memoizedState,r=s!==null,n||s===null||(s=e.child.sibling,s!==null&&(d=t.firstEffect,d!==null?(t.firstEffect=s,s.nextEffect=d):(t.firstEffect=t.lastEffect=s,s.nextEffect=null),s.effectTag=8))),n&&!r&&t.mode&2&&(e===null&&t.memoizedProps.unstable_avoidThisFallback!==!0||et.current&1?pt===pr&&(pt=wl):((pt===pr||pt===wl)&&(pt=xl),Ko!==0&&Nt!==null&&(xr(Nt,bt),Tu(Nt,Ko)))),(n||r)&&(t.effectTag|=4),null);case 4:return Qr(),ea(t),null;case 10:return zo(t),null;case 17:return dt(t.type)&&Nn(),null;case 19:if(We(et),r=t.memoizedState,r===null)return null;if(s=(t.effectTag&64)!==0,d=r.rendering,d===null){if(s)gl(r,!1);else if(pt!==pr||e!==null&&e.effectTag&64)for(d=t.child;d!==null;){if(e=al(d),e!==null){for(t.effectTag|=64,gl(r,!1),s=e.updateQueue,s!==null&&(t.updateQueue=s,t.effectTag|=4),r.lastEffect===null&&(t.firstEffect=null),t.lastEffect=r.lastEffect,r=t.child;r!==null;)s=r,d=n,s.effectTag&=2,s.nextEffect=null,s.firstEffect=null,s.lastEffect=null,e=s.alternate,e===null?(s.childExpirationTime=0,s.expirationTime=d,s.child=null,s.memoizedProps=null,s.memoizedState=null,s.updateQueue=null,s.dependencies=null):(s.childExpirationTime=e.childExpirationTime,s.expirationTime=e.expirationTime,s.child=e.child,s.memoizedProps=e.memoizedProps,s.memoizedState=e.memoizedState,s.updateQueue=e.updateQueue,d=e.dependencies,s.dependencies=d===null?null:{expirationTime:d.expirationTime,firstContext:d.firstContext,responders:d.responders}),r=r.sibling;return Ge(et,et.current&1|2),t.child}d=d.sibling}}else{if(!s)if(e=al(d),e!==null){if(t.effectTag|=64,s=!0,n=e.updateQueue,n!==null&&(t.updateQueue=n,t.effectTag|=4),gl(r,!0),r.tail===null&&r.tailMode==="hidden"&&!d.alternate)return t=t.lastEffect=r.lastEffect,t!==null&&(t.nextEffect=null),null}else 2*me()-r.renderingStartTime>r.tailExpiration&&1<n&&(t.effectTag|=64,s=!0,gl(r,!1),t.expirationTime=t.childExpirationTime=n-1);r.isBackwards?(d.sibling=t.child,t.child=d):(n=r.last,n!==null?n.sibling=d:t.child=d,r.last=d)}return r.tail!==null?(r.tailExpiration===0&&(r.tailExpiration=me()+500),n=r.tail,r.rendering=n,r.tail=n.sibling,r.lastEffect=t.lastEffect,r.renderingStartTime=me(),n.sibling=null,t=et.current,Ge(et,s?t&1|2:t&1),n):null}throw Error(p(156,t.tag))}i(Au,"si");function Iu(e){switch(e.tag){case 1:dt(e.type)&&Nn();var t=e.effectTag;return t&4096?(e.effectTag=t&-4097|64,e):null;case 3:if(Qr(),We(nt),We(it),t=e.effectTag,t&64)throw Error(p(285));return e.effectTag=t&-4097|64,e;case 5:return Fs(e),null;case 13:return We(et),t=e.effectTag,t&4096?(e.effectTag=t&-4097|64,e):null;case 19:return We(et),null;case 4:return Qr(),null;case 10:return zo(e),null;default:return null}}i(Iu,"zi");function ta(e,t){return{value:e,source:t,stack:no(t)}}i(ta,"Ai");var Hu=typeof WeakSet=="function"?WeakSet:Set;function na(e,t){var n=t.source,r=t.stack;r===null&&n!==null&&(r=no(n)),n!==null&&Rt(n.type),t=t.value,e!==null&&e.tag===1&&Rt(e.type);try{console.error(t)}catch(s){setTimeout(function(){throw s})}}i(na,"Ci");function Fu(e,t){try{t.props=e.memoizedProps,t.state=e.memoizedState,t.componentWillUnmount()}catch(n){Cr(e,n)}}i(Fu,"Di");function ru(e){var t=e.ref;if(t!==null)if(typeof t=="function")try{t(null)}catch(n){Cr(e,n)}else t.current=null}i(ru,"Fi");function Vu(e,t){switch(t.tag){case 0:case 11:case 15:case 22:return;case 1:if(t.effectTag&256&&e!==null){var n=e.memoizedProps,r=e.memoizedState;e=t.stateNode,t=e.getSnapshotBeforeUpdate(t.elementType===t.type?n:lt(t.type,n),r),e.__reactInternalSnapshotBeforeUpdate=t}return;case 3:case 5:case 6:case 4:case 17:return}throw Error(p(163))}i(Vu,"Gi");function ou(e,t){if(t=t.updateQueue,t=t!==null?t.lastEffect:null,t!==null){var n=t=t.next;do{if((n.tag&e)===e){var r=n.destroy;n.destroy=void 0,r!==void 0&&r()}n=n.next}while(n!==t)}}i(ou,"Hi");function iu(e,t){if(t=t.updateQueue,t=t!==null?t.lastEffect:null,t!==null){var n=t=t.next;do{if((n.tag&e)===e){var r=n.create;n.destroy=r()}n=n.next}while(n!==t)}}i(iu,"Ii");function $u(e,t,n){switch(n.tag){case 0:case 11:case 15:case 22:iu(3,n);return;case 1:if(e=n.stateNode,n.effectTag&4)if(t===null)e.componentDidMount();else{var r=n.elementType===n.type?t.memoizedProps:lt(n.type,t.memoizedProps);e.componentDidUpdate(r,t.memoizedState,e.__reactInternalSnapshotBeforeUpdate)}t=n.updateQueue,t!==null&&Sa(n,t,e);return;case 3:if(t=n.updateQueue,t!==null){if(e=null,n.child!==null)switch(n.child.tag){case 5:e=n.child.stateNode;break;case 1:e=n.child.stateNode}Sa(n,t,e)}return;case 5:e=n.stateNode,t===null&&n.effectTag&4&&Ai(n.type,n.memoizedProps)&&e.focus();return;case 6:return;case 4:return;case 12:return;case 13:n.memoizedState===null&&(n=n.alternate,n!==null&&(n=n.memoizedState,n!==null&&(n=n.dehydrated,n!==null&&Xn(n))));return;case 19:case 17:case 20:case 21:return}throw Error(p(163))}i($u,"Ji");function lu(e,t,n){switch(typeof pa=="function"&&pa(t),t.tag){case 0:case 11:case 14:case 15:case 22:if(e=t.updateQueue,e!==null&&(e=e.lastEffect,e!==null)){var r=e.next;Te(97<n?97:n,function(){var s=r;do{var d=s.destroy;if(d!==void 0){var m=t;try{d()}catch(v){Cr(m,v)}}s=s.next}while(s!==r)})}break;case 1:ru(t),n=t.stateNode,typeof n.componentWillUnmount=="function"&&Fu(t,n);break;case 5:ru(t);break;case 4:cu(e,t,n)}}i(lu,"Ki");function su(e){var t=e.alternate;e.return=null,e.child=null,e.memoizedState=null,e.updateQueue=null,e.dependencies=null,e.alternate=null,e.firstEffect=null,e.lastEffect=null,e.pendingProps=null,e.memoizedProps=null,e.stateNode=null,t!==null&&su(t)}i(su,"Ni");function au(e){return e.tag===5||e.tag===3||e.tag===4}i(au,"Oi");function uu(e){e:{for(var t=e.return;t!==null;){if(au(t)){var n=t;break e}t=t.return}throw Error(p(160))}switch(t=n.stateNode,n.tag){case 5:var r=!1;break;case 3:t=t.containerInfo,r=!0;break;case 4:t=t.containerInfo,r=!0;break;default:throw Error(p(161))}n.effectTag&16&&(Zn(t,""),n.effectTag&=-17);e:t:for(n=e;;){for(;n.sibling===null;){if(n.return===null||au(n.return)){n=null;break e}n=n.return}for(n.sibling.return=n.return,n=n.sibling;n.tag!==5&&n.tag!==6&&n.tag!==18;){if(n.effectTag&2||n.child===null||n.tag===4)continue t;n.child.return=n,n=n.child}if(!(n.effectTag&2)){n=n.stateNode;break e}}r?ra(e,n,t):oa(e,n,t)}i(uu,"Pi");function ra(e,t,n){var r=e.tag,s=r===5||r===6;if(s)e=s?e.stateNode:e.stateNode.instance,t?n.nodeType===8?n.parentNode.insertBefore(e,t):n.insertBefore(e,t):(n.nodeType===8?(t=n.parentNode,t.insertBefore(e,n)):(t=n,t.appendChild(e)),n=n._reactRootContainer,n!=null||t.onclick!==null||(t.onclick=er));else if(r!==4&&(e=e.child,e!==null))for(ra(e,t,n),e=e.sibling;e!==null;)ra(e,t,n),e=e.sibling}i(ra,"Qi");function oa(e,t,n){var r=e.tag,s=r===5||r===6;if(s)e=s?e.stateNode:e.stateNode.instance,t?n.insertBefore(e,t):n.appendChild(e);else if(r!==4&&(e=e.child,e!==null))for(oa(e,t,n),e=e.sibling;e!==null;)oa(e,t,n),e=e.sibling}i(oa,"Ri");function cu(e,t,n){for(var r=t,s=!1,d,m;;){if(!s){s=r.return;e:for(;;){if(s===null)throw Error(p(160));switch(d=s.stateNode,s.tag){case 5:m=!1;break e;case 3:d=d.containerInfo,m=!0;break e;case 4:d=d.containerInfo,m=!0;break e}s=s.return}s=!0}if(r.tag===5||r.tag===6){e:for(var v=e,L=r,S=n,te=L;;)if(lu(v,te,S),te.child!==null&&te.tag!==4)te.child.return=te,te=te.child;else{if(te===L)break e;for(;te.sibling===null;){if(te.return===null||te.return===L)break e;te=te.return}te.sibling.return=te.return,te=te.sibling}m?(v=d,L=r.stateNode,v.nodeType===8?v.parentNode.removeChild(L):v.removeChild(L)):d.removeChild(r.stateNode)}else if(r.tag===4){if(r.child!==null){d=r.stateNode.containerInfo,m=!0,r.child.return=r,r=r.child;continue}}else if(lu(e,r,n),r.child!==null){r.child.return=r,r=r.child;continue}if(r===t)break;for(;r.sibling===null;){if(r.return===null||r.return===t)return;r=r.return,r.tag===4&&(s=!1)}r.sibling.return=r.return,r=r.sibling}}i(cu,"Mi");function ia(e,t){switch(t.tag){case 0:case 11:case 14:case 15:case 22:ou(3,t);return;case 1:return;case 5:var n=t.stateNode;if(n!=null){var r=t.memoizedProps,s=e!==null?e.memoizedProps:r;e=t.type;var d=t.updateQueue;if(t.updateQueue=null,d!==null){for(n[Pr]=r,e==="input"&&r.type==="radio"&&r.name!=null&&Vl(n,r),wo(e,s),t=wo(e,r),s=0;s<d.length;s+=2){var m=d[s],v=d[s+1];m==="style"?yo(n,v):m==="dangerouslySetInnerHTML"?gi(n,v):m==="children"?Zn(n,v):Jr(n,m,v,t)}switch(e){case"input":ai(n,r);break;case"textarea":mi(n,r);break;case"select":t=n._wrapperState.wasMultiple,n._wrapperState.wasMultiple=!!r.multiple,e=r.value,e!=null?Wn(n,!!r.multiple,e,!1):t!==!!r.multiple&&(r.defaultValue!=null?Wn(n,!!r.multiple,r.defaultValue,!0):Wn(n,!!r.multiple,r.multiple?[]:"",!1))}}}return;case 6:if(t.stateNode===null)throw Error(p(162));t.stateNode.nodeValue=t.memoizedProps;return;case 3:t=t.stateNode,t.hydrate&&(t.hydrate=!1,Xn(t.containerInfo));return;case 12:return;case 13:if(n=t,t.memoizedState===null?r=!1:(r=!0,n=t.child,aa=me()),n!==null)e:for(e=n;;){if(e.tag===5)d=e.stateNode,r?(d=d.style,typeof d.setProperty=="function"?d.setProperty("display","none","important"):d.display="none"):(d=e.stateNode,s=e.memoizedProps.style,s=s!=null&&s.hasOwnProperty("display")?s.display:null,d.style.display=Pi("display",s));else if(e.tag===6)e.stateNode.nodeValue=r?"":e.memoizedProps;else if(e.tag===13&&e.memoizedState!==null&&e.memoizedState.dehydrated===null){d=e.child.sibling,d.return=e,e=d;continue}else if(e.child!==null){e.child.return=e,e=e.child;continue}if(e===n)break;for(;e.sibling===null;){if(e.return===null||e.return===n)break e;e=e.return}e.sibling.return=e.return,e=e.sibling}du(t);return;case 19:du(t);return;case 17:return}throw Error(p(163))}i(ia,"Si");function du(e){var t=e.updateQueue;if(t!==null){e.updateQueue=null;var n=e.stateNode;n===null&&(n=e.stateNode=new Hu),t.forEach(function(r){var s=Yu.bind(null,e,r);n.has(r)||(n.add(r),r.then(s,s))})}}i(du,"Ui");var zu=typeof WeakMap=="function"?WeakMap:Map;function fu(e,t,n){n=On(n,null),n.tag=3,n.payload={element:null};var r=t.value;return n.callback=function(){_l||(_l=!0,ua=r),na(e,t)},n}i(fu,"Xi");function mu(e,t,n){n=On(n,null),n.tag=3;var r=e.type.getDerivedStateFromError;if(typeof r=="function"){var s=t.value;n.payload=function(){return na(e,t),r(s)}}var d=e.stateNode;return d!==null&&typeof d.componentDidCatch=="function"&&(n.callback=function(){typeof r!="function"&&(Hn===null?Hn=new Set([this]):Hn.add(this),na(e,t));var m=t.stack;this.componentDidCatch(t.value,{componentStack:m!==null?m:""})}),n}i(mu,"$i");var Bu=Math.ceil,yl=kt.ReactCurrentDispatcher,pu=kt.ReactCurrentOwner,mt=0,la=8,Ut=16,en=32,pr=0,Cl=1,hu=2,wl=3,xl=4,sa=5,Ee=mt,Nt=null,Me=null,bt=0,pt=pr,El=null,wn=1073741823,Qo=1073741823,kl=null,Ko=0,bl=!1,aa=0,vu=500,ce=null,_l=!1,ua=null,Hn=null,Ll=!1,Yo=null,Go=90,hr=null,Xo=0,ca=null,Sl=0;function tn(){return(Ee&(Ut|en))!==mt?1073741821-(me()/10|0):Sl!==0?Sl:Sl=1073741821-(me()/10|0)}i(tn,"Gg");function vr(e,t,n){if(t=t.mode,!(t&2))return 1073741823;var r=Pe();if(!(t&4))return r===99?1073741823:1073741822;if((Ee&Ut)!==mt)return bt;if(n!==null)e=_e(e,n.timeoutMs|0||5e3,250);else switch(r){case 99:e=1073741823;break;case 98:e=_e(e,150,100);break;case 97:case 96:e=_e(e,5e3,250);break;case 95:e=2;break;default:throw Error(p(326))}return Nt!==null&&e===bt&&--e,e}i(vr,"Hg");function Fn(e,t){if(50<Xo)throw Xo=0,ca=null,Error(p(185));if(e=Tl(e,t),e!==null){var n=Pe();t===1073741823?(Ee&la)!==mt&&(Ee&(Ut|en))===mt?da(e):(Pt(e),Ee===mt&&Ye()):Pt(e),(Ee&4)===mt||n!==98&&n!==99||(hr===null?hr=new Map([[e,t]]):(n=hr.get(e),(n===void 0||n>t)&&hr.set(e,t)))}}i(Fn,"Ig");function Tl(e,t){e.expirationTime<t&&(e.expirationTime=t);var n=e.alternate;n!==null&&n.expirationTime<t&&(n.expirationTime=t);var r=e.return,s=null;if(r===null&&e.tag===3)s=e.stateNode;else for(;r!==null;){if(n=r.alternate,r.childExpirationTime<t&&(r.childExpirationTime=t),n!==null&&n.childExpirationTime<t&&(n.childExpirationTime=t),r.return===null&&r.tag===3){s=r.stateNode;break}r=r.return}return s!==null&&(Nt===s&&(Nl(t),pt===xl&&xr(s,bt)),Tu(s,t)),s}i(Tl,"xj");function Ml(e){var t=e.lastExpiredTime;if(t!==0||(t=e.firstPendingTime,!Su(e,t)))return t;var n=e.lastPingedTime;return e=e.nextKnownPendingLevel,e=n>e?n:e,2>=e&&t!==e?0:e}i(Ml,"zj");function Pt(e){if(e.lastExpiredTime!==0)e.callbackExpirationTime=1073741823,e.callbackPriority=99,e.callbackNode=Be(da.bind(null,e));else{var t=Ml(e),n=e.callbackNode;if(t===0)n!==null&&(e.callbackNode=null,e.callbackExpirationTime=0,e.callbackPriority=90);else{var r=tn();if(t===1073741823?r=99:t===1||t===2?r=95:(r=10*(1073741821-t)-10*(1073741821-r),r=0>=r?99:250>=r?98:5250>=r?97:95),n!==null){var s=e.callbackPriority;if(e.callbackExpirationTime===t&&s>=r)return;n!==C&&jr(n)}e.callbackExpirationTime=t,e.callbackPriority=r,t=t===1073741823?Be(da.bind(null,e)):Ze(r,gu.bind(null,e),{timeout:10*(1073741821-t)-me()}),e.callbackNode=t}}}i(Pt,"Z");function gu(e,t){if(Sl=0,t)return t=tn(),ya(e,t),Pt(e),null;var n=Ml(e);if(n!==0){if(t=e.callbackNode,(Ee&(Ut|en))!==mt)throw Error(p(327));if(Gr(),e===Nt&&n===bt||gr(e,n),Me!==null){var r=Ee;Ee|=Ut;var s=xu();do try{Wu();break}catch(v){wu(e,v)}while(!0);if(Wr(),Ee=r,yl.current=s,pt===Cl)throw t=El,gr(e,n),xr(e,n),Pt(e),t;if(Me===null)switch(s=e.finishedWork=e.current.alternate,e.finishedExpirationTime=n,r=pt,Nt=null,r){case pr:case Cl:throw Error(p(345));case hu:ya(e,2<n?2:n);break;case wl:if(xr(e,n),r=e.lastSuspendedTime,n===r&&(e.nextKnownPendingLevel=fa(s)),wn===1073741823&&(s=aa+vu-me(),10<s)){if(bl){var d=e.lastPingedTime;if(d===0||d>=n){e.lastPingedTime=n,gr(e,n);break}}if(d=Ml(e),d!==0&&d!==n)break;if(r!==0&&r!==n){e.lastPingedTime=r;break}e.timeoutHandle=To(yr.bind(null,e),s);break}yr(e);break;case xl:if(xr(e,n),r=e.lastSuspendedTime,n===r&&(e.nextKnownPendingLevel=fa(s)),bl&&(s=e.lastPingedTime,s===0||s>=n)){e.lastPingedTime=n,gr(e,n);break}if(s=Ml(e),s!==0&&s!==n)break;if(r!==0&&r!==n){e.lastPingedTime=r;break}if(Qo!==1073741823?r=10*(1073741821-Qo)-me():wn===1073741823?r=0:(r=10*(1073741821-wn)-5e3,s=me(),n=10*(1073741821-n)-s,r=s-r,0>r&&(r=0),r=(120>r?120:480>r?480:1080>r?1080:1920>r?1920:3e3>r?3e3:4320>r?4320:1960*Bu(r/1960))-r,n<r&&(r=n)),10<r){e.timeoutHandle=To(yr.bind(null,e),r);break}yr(e);break;case sa:if(wn!==1073741823&&kl!==null){d=wn;var m=kl;if(r=m.busyMinDurationMs|0,0>=r?r=0:(s=m.busyDelayMs|0,d=me()-(10*(1073741821-d)-(m.timeoutMs|0||5e3)),r=d<=s?0:s+r-d),10<r){xr(e,n),e.timeoutHandle=To(yr.bind(null,e),r);break}}yr(e);break;default:throw Error(p(329))}if(Pt(e),e.callbackNode===t)return gu.bind(null,e)}}return null}i(gu,"Bj");function da(e){var t=e.lastExpiredTime;if(t=t!==0?t:1073741823,(Ee&(Ut|en))!==mt)throw Error(p(327));if(Gr(),e===Nt&&t===bt||gr(e,t),Me!==null){var n=Ee;Ee|=Ut;var r=xu();do try{Uu();break}catch(s){wu(e,s)}while(!0);if(Wr(),Ee=n,yl.current=r,pt===Cl)throw n=El,gr(e,t),xr(e,t),Pt(e),n;if(Me!==null)throw Error(p(261));e.finishedWork=e.current.alternate,e.finishedExpirationTime=t,Nt=null,yr(e),Pt(e)}return null}i(da,"yj");function ju(){if(hr!==null){var e=hr;hr=null,e.forEach(function(t,n){ya(n,t),Pt(n)}),Ye()}}i(ju,"Lj");function yu(e,t){var n=Ee;Ee|=1;try{return e(t)}finally{Ee=n,Ee===mt&&Ye()}}i(yu,"Mj");function Cu(e,t){var n=Ee;Ee&=-2,Ee|=la;try{return e(t)}finally{Ee=n,Ee===mt&&Ye()}}i(Cu,"Nj");function gr(e,t){e.finishedWork=null,e.finishedExpirationTime=0;var n=e.timeoutHandle;if(n!==-1&&(e.timeoutHandle=-1,es(n)),Me!==null)for(n=Me.return;n!==null;){var r=n;switch(r.tag){case 1:r=r.type.childContextTypes,r!=null&&Nn();break;case 3:Qr(),We(nt),We(it);break;case 5:Fs(r);break;case 4:Qr();break;case 13:We(et);break;case 19:We(et);break;case 10:zo(r)}n=n.return}Nt=e,Me=wr(e.current,null),bt=t,pt=pr,El=null,Qo=wn=1073741823,kl=null,Ko=0,bl=!1}i(gr,"Ej");function wu(e,t){do{try{if(Wr(),ul.current=hl,cl)for(var n=st.memoizedState;n!==null;){var r=n.queue;r!==null&&(r.pending=null),n=n.next}if(An=0,wt=Ct=st=null,cl=!1,Me===null||Me.return===null)return pt=Cl,El=t,Me=null;e:{var s=e,d=Me.return,m=Me,v=t;if(t=bt,m.effectTag|=2048,m.firstEffect=m.lastEffect=null,v!==null&&typeof v=="object"&&typeof v.then=="function"){var L=v;if(!(m.mode&2)){var S=m.alternate;S?(m.updateQueue=S.updateQueue,m.memoizedState=S.memoizedState,m.expirationTime=S.expirationTime):(m.updateQueue=null,m.memoizedState=null)}var te=(et.current&1)!==0,ie=d;do{var Ie;if(Ie=ie.tag===13){var je=ie.memoizedState;if(je!==null)Ie=je.dehydrated!==null;else{var Ft=ie.memoizedProps;Ie=Ft.fallback===void 0?!1:Ft.unstable_avoidThisFallback!==!0?!0:!te}}if(Ie){var vt=ie.updateQueue;if(vt===null){var k=new Set;k.add(L),ie.updateQueue=k}else vt.add(L);if(!(ie.mode&2)){if(ie.effectTag|=64,m.effectTag&=-2981,m.tag===1)if(m.alternate===null)m.tag=17;else{var x=On(1073741823,null);x.tag=2,Dn(m,x)}m.expirationTime=1073741823;break e}v=void 0,m=t;var M=s.pingCache;if(M===null?(M=s.pingCache=new zu,v=new Set,M.set(L,v)):(v=M.get(L),v===void 0&&(v=new Set,M.set(L,v))),!v.has(m)){v.add(m);var U=Ku.bind(null,s,L,m);L.then(U,U)}ie.effectTag|=4096,ie.expirationTime=t;break e}ie=ie.return}while(ie!==null);v=Error((Rt(m.type)||"A React component")+` suspended while rendering, but no fallback UI was specified.

Add a <Suspense fallback=...> component higher in the tree to provide a loading indicator or placeholder to display.`+no(m))}pt!==sa&&(pt=hu),v=ta(v,m),ie=d;do{switch(ie.tag){case 3:L=v,ie.effectTag|=4096,ie.expirationTime=t;var ee=fu(ie,L,t);La(ie,ee);break e;case 1:L=v;var le=ie.type,ye=ie.stateNode;if(!(ie.effectTag&64)&&(typeof le.getDerivedStateFromError=="function"||ye!==null&&typeof ye.componentDidCatch=="function"&&(Hn===null||!Hn.has(ye)))){ie.effectTag|=4096,ie.expirationTime=t;var Fe=mu(ie,L,t);La(ie,Fe);break e}}ie=ie.return}while(ie!==null)}Me=bu(Me)}catch(Je){t=Je;continue}break}while(!0)}i(wu,"Hj");function xu(){var e=yl.current;return yl.current=hl,e===null?hl:e}i(xu,"Fj");function Eu(e,t){e<wn&&2<e&&(wn=e),t!==null&&e<Qo&&2<e&&(Qo=e,kl=t)}i(Eu,"Ag");function Nl(e){e>Ko&&(Ko=e)}i(Nl,"Bg");function Uu(){for(;Me!==null;)Me=ku(Me)}i(Uu,"Kj");function Wu(){for(;Me!==null&&!E();)Me=ku(Me)}i(Wu,"Gj");function ku(e){var t=Lu(e.alternate,e,bt);return e.memoizedProps=e.pendingProps,t===null&&(t=bu(e)),pu.current=null,t}i(ku,"Qj");function bu(e){Me=e;do{var t=Me.alternate;if(e=Me.return,Me.effectTag&2048){if(t=Iu(Me),t!==null)return t.effectTag&=2047,t;e!==null&&(e.firstEffect=e.lastEffect=null,e.effectTag|=2048)}else{if(t=Au(t,Me,bt),bt===1||Me.childExpirationTime!==1){for(var n=0,r=Me.child;r!==null;){var s=r.expirationTime,d=r.childExpirationTime;s>n&&(n=s),d>n&&(n=d),r=r.sibling}Me.childExpirationTime=n}if(t!==null)return t;e!==null&&!(e.effectTag&2048)&&(e.firstEffect===null&&(e.firstEffect=Me.firstEffect),Me.lastEffect!==null&&(e.lastEffect!==null&&(e.lastEffect.nextEffect=Me.firstEffect),e.lastEffect=Me.lastEffect),1<Me.effectTag&&(e.lastEffect!==null?e.lastEffect.nextEffect=Me:e.firstEffect=Me,e.lastEffect=Me))}if(t=Me.sibling,t!==null)return t;Me=e}while(Me!==null);return pt===pr&&(pt=sa),null}i(bu,"Pj");function fa(e){var t=e.expirationTime;return e=e.childExpirationTime,t>e?t:e}i(fa,"Ij");function yr(e){var t=Pe();return Te(99,Zu.bind(null,e,t)),null}i(yr,"Jj");function Zu(e,t){do Gr();while(Yo!==null);if((Ee&(Ut|en))!==mt)throw Error(p(327));var n=e.finishedWork,r=e.finishedExpirationTime;if(n===null)return null;if(e.finishedWork=null,e.finishedExpirationTime=0,n===e.current)throw Error(p(177));e.callbackNode=null,e.callbackExpirationTime=0,e.callbackPriority=90,e.nextKnownPendingLevel=0;var s=fa(n);if(e.firstPendingTime=s,r<=e.lastSuspendedTime?e.firstSuspendedTime=e.lastSuspendedTime=e.nextKnownPendingLevel=0:r<=e.firstSuspendedTime&&(e.firstSuspendedTime=r-1),r<=e.lastPingedTime&&(e.lastPingedTime=0),r<=e.lastExpiredTime&&(e.lastExpiredTime=0),e===Nt&&(Me=Nt=null,bt=0),1<n.effectTag?n.lastEffect!==null?(n.lastEffect.nextEffect=n,s=n.firstEffect):s=n:s=n.firstEffect,s!==null){var d=Ee;Ee|=en,pu.current=null,_o=go;var m=Oi();if(mn(m)){if("selectionStart"in m)var v={start:m.selectionStart,end:m.selectionEnd};else e:{v=(v=m.ownerDocument)&&v.defaultView||window;var L=v.getSelection&&v.getSelection();if(L&&L.rangeCount!==0){v=L.anchorNode;var S=L.anchorOffset,te=L.focusNode;L=L.focusOffset;try{v.nodeType,te.nodeType}catch{v=null;break e}var ie=0,Ie=-1,je=-1,Ft=0,vt=0,k=m,x=null;t:for(;;){for(var M;k!==v||S!==0&&k.nodeType!==3||(Ie=ie+S),k!==te||L!==0&&k.nodeType!==3||(je=ie+L),k.nodeType===3&&(ie+=k.nodeValue.length),(M=k.firstChild)!==null;)x=k,k=M;for(;;){if(k===m)break t;if(x===v&&++Ft===S&&(Ie=ie),x===te&&++vt===L&&(je=ie),(M=k.nextSibling)!==null)break;k=x,x=k.parentNode}k=M}v=Ie===-1||je===-1?null:{start:Ie,end:je}}else v=null}v=v||{start:0,end:0}}else v=null;Lo={activeElementDetached:null,focusedElem:m,selectionRange:v},go=!1,ce=s;do try{qu()}catch(Re){if(ce===null)throw Error(p(330));Cr(ce,Re),ce=ce.nextEffect}while(ce!==null);ce=s;do try{for(m=e,v=t;ce!==null;){var U=ce.effectTag;if(U&16&&Zn(ce.stateNode,""),U&128){var ee=ce.alternate;if(ee!==null){var le=ee.ref;le!==null&&(typeof le=="function"?le(null):le.current=null)}}switch(U&1038){case 2:uu(ce),ce.effectTag&=-3;break;case 6:uu(ce),ce.effectTag&=-3,ia(ce.alternate,ce);break;case 1024:ce.effectTag&=-1025;break;case 1028:ce.effectTag&=-1025,ia(ce.alternate,ce);break;case 4:ia(ce.alternate,ce);break;case 8:S=ce,cu(m,S,v),su(S)}ce=ce.nextEffect}}catch(Re){if(ce===null)throw Error(p(330));Cr(ce,Re),ce=ce.nextEffect}while(ce!==null);if(le=Lo,ee=Oi(),U=le.focusedElem,v=le.selectionRange,ee!==U&&U&&U.ownerDocument&&Eo(U.ownerDocument.documentElement,U)){for(v!==null&&mn(U)&&(ee=v.start,le=v.end,le===void 0&&(le=ee),"selectionStart"in U?(U.selectionStart=ee,U.selectionEnd=Math.min(le,U.value.length)):(le=(ee=U.ownerDocument||document)&&ee.defaultView||window,le.getSelection&&(le=le.getSelection(),S=U.textContent.length,m=Math.min(v.start,S),v=v.end===void 0?m:Math.min(v.end,S),!le.extend&&m>v&&(S=v,v=m,m=S),S=Jl(U,m),te=Jl(U,v),S&&te&&(le.rangeCount!==1||le.anchorNode!==S.node||le.anchorOffset!==S.offset||le.focusNode!==te.node||le.focusOffset!==te.offset)&&(ee=ee.createRange(),ee.setStart(S.node,S.offset),le.removeAllRanges(),m>v?(le.addRange(ee),le.extend(te.node,te.offset)):(ee.setEnd(te.node,te.offset),le.addRange(ee)))))),ee=[],le=U;le=le.parentNode;)le.nodeType===1&&ee.push({element:le,left:le.scrollLeft,top:le.scrollTop});for(typeof U.focus=="function"&&U.focus(),U=0;U<ee.length;U++)le=ee[U],le.element.scrollLeft=le.left,le.element.scrollTop=le.top}go=!!_o,Lo=_o=null,e.current=n,ce=s;do try{for(U=e;ce!==null;){var ye=ce.effectTag;if(ye&36&&$u(U,ce.alternate,ce),ye&128){ee=void 0;var Fe=ce.ref;if(Fe!==null){var Je=ce.stateNode;switch(ce.tag){case 5:ee=Je;break;default:ee=Je}typeof Fe=="function"?Fe(ee):Fe.current=ee}}ce=ce.nextEffect}}catch(Re){if(ce===null)throw Error(p(330));Cr(ce,Re),ce=ce.nextEffect}while(ce!==null);ce=null,R(),Ee=d}else e.current=n;if(Ll)Ll=!1,Yo=e,Go=t;else for(ce=s;ce!==null;)t=ce.nextEffect,ce.nextEffect=null,ce=t;if(t=e.firstPendingTime,t===0&&(Hn=null),t===1073741823?e===ca?Xo++:(Xo=0,ca=e):Xo=0,typeof ma=="function"&&ma(n.stateNode,r),Pt(e),_l)throw _l=!1,e=ua,ua=null,e;return(Ee&la)!==mt||Ye(),null}i(Zu,"Sj");function qu(){for(;ce!==null;){var e=ce.effectTag;e&256&&Vu(ce.alternate,ce),!(e&512)||Ll||(Ll=!0,Ze(97,function(){return Gr(),null})),ce=ce.nextEffect}}i(qu,"Tj");function Gr(){if(Go!==90){var e=97<Go?97:Go;return Go=90,Te(e,Qu)}}i(Gr,"Dj");function Qu(){if(Yo===null)return!1;var e=Yo;if(Yo=null,(Ee&(Ut|en))!==mt)throw Error(p(331));var t=Ee;for(Ee|=en,e=e.current.firstEffect;e!==null;){try{var n=e;if(n.effectTag&512)switch(n.tag){case 0:case 11:case 15:case 22:ou(5,n),iu(5,n)}}catch(r){if(e===null)throw Error(p(330));Cr(e,r)}n=e.nextEffect,e.nextEffect=null,e=n}return Ee=t,Ye(),!0}i(Qu,"Vj");function _u(e,t,n){t=ta(n,t),t=fu(e,t,1073741823),Dn(e,t),e=Tl(e,1073741823),e!==null&&Pt(e)}i(_u,"Wj");function Cr(e,t){if(e.tag===3)_u(e,e,t);else for(var n=e.return;n!==null;){if(n.tag===3){_u(n,e,t);break}else if(n.tag===1){var r=n.stateNode;if(typeof n.type.getDerivedStateFromError=="function"||typeof r.componentDidCatch=="function"&&(Hn===null||!Hn.has(r))){e=ta(t,e),e=mu(n,e,1073741823),Dn(n,e),n=Tl(n,1073741823),n!==null&&Pt(n);break}}n=n.return}}i(Cr,"Ei");function Ku(e,t,n){var r=e.pingCache;r!==null&&r.delete(t),Nt===e&&bt===n?pt===xl||pt===wl&&wn===1073741823&&me()-aa<vu?gr(e,bt):bl=!0:Su(e,n)&&(t=e.lastPingedTime,t!==0&&t<n||(e.lastPingedTime=n,Pt(e)))}i(Ku,"Oj");function Yu(e,t){var n=e.stateNode;n!==null&&n.delete(t),t=0,t===0&&(t=tn(),t=vr(t,e,null)),e=Tl(e,t),e!==null&&Pt(e)}i(Yu,"Vi");var Lu;Lu=i(function(e,t,n){var r=t.expirationTime;if(e!==null){var s=t.pendingProps;if(e.memoizedProps!==s||nt.current)Jt=!0;else{if(r<n){switch(Jt=!1,t.tag){case 3:Ya(t),Ks();break;case 5:if(Oa(t),t.mode&4&&n!==1&&s.hidden)return t.expirationTime=t.childExpirationTime=1,null;break;case 1:dt(t.type)&&Pn(t);break;case 4:Hs(t,t.stateNode.containerInfo);break;case 10:r=t.memoizedProps.value,s=t.type._context,Ge(At,s._currentValue),s._currentValue=r;break;case 13:if(t.memoizedState!==null)return r=t.child.childExpirationTime,r!==0&&r>=n?Ga(e,t,n):(Ge(et,et.current&1),t=Cn(e,t,n),t!==null?t.sibling:null);Ge(et,et.current&1);break;case 19:if(r=t.childExpirationTime>=n,e.effectTag&64){if(r)return Ja(e,t,n);t.effectTag|=64}if(s=t.memoizedState,s!==null&&(s.rendering=null,s.tail=null),Ge(et,et.current),!r)return null}return Cn(e,t,n)}Jt=!1}}else Jt=!1;switch(t.expirationTime=0,t.tag){case 2:if(r=t.type,e!==null&&(e.alternate=null,t.alternate=null,t.effectTag|=2),e=t.pendingProps,s=Yt(t,it.current),Zr(t,n),s=zs(null,t,r,e,s,n),t.effectTag|=1,typeof s=="object"&&s!==null&&typeof s.render=="function"&&s.$$typeof===void 0){if(t.tag=1,t.memoizedState=null,t.updateQueue=null,dt(r)){var d=!0;Pn(t)}else d=!1;t.memoizedState=s.state!==null&&s.state!==void 0?s.state:null,Os(t);var m=r.getDerivedStateFromProps;typeof m=="function"&&ol(t,r,m,e),s.updater=il,t.stateNode=s,s._reactInternalFiber=t,As(t,r,e,n),t=Gs(null,t,r,!0,d,n)}else t.tag=0,Mt(null,t,s,n),t=t.child;return t;case 16:e:{if(s=t.elementType,e!==null&&(e.alternate=null,t.alternate=null,t.effectTag|=2),e=t.pendingProps,Hl(s),s._status!==1)throw s._result;switch(s=s._result,t.type=s,d=t.tag=Ju(s),e=lt(s,e),d){case 0:t=Ys(null,t,s,e,n);break e;case 1:t=Ka(null,t,s,e,n);break e;case 11:t=Wa(null,t,s,e,n);break e;case 14:t=Za(null,t,s,lt(s.type,e),r,n);break e}throw Error(p(306,s,""))}return t;case 0:return r=t.type,s=t.pendingProps,s=t.elementType===r?s:lt(r,s),Ys(e,t,r,s,n);case 1:return r=t.type,s=t.pendingProps,s=t.elementType===r?s:lt(r,s),Ka(e,t,r,s,n);case 3:if(Ya(t),r=t.updateQueue,e===null||r===null)throw Error(p(282));if(r=t.pendingProps,s=t.memoizedState,s=s!==null?s.element:null,Ds(e,t),Bo(t,r,null,n),r=t.memoizedState.element,r===s)Ks(),t=Cn(e,t,n);else{if((s=t.stateNode.hydrate)&&(In=pn(t.stateNode.containerInfo.firstChild),yn=t,s=mr=!0),s)for(n=Is(t,null,r,n),t.child=n;n;)n.effectTag=n.effectTag&-3|1024,n=n.sibling;else Mt(e,t,r,n),Ks();t=t.child}return t;case 5:return Oa(t),e===null&&Qs(t),r=t.type,s=t.pendingProps,d=e!==null?e.memoizedProps:null,m=s.children,So(r,s)?m=null:d!==null&&So(r,d)&&(t.effectTag|=16),Qa(e,t),t.mode&4&&n!==1&&s.hidden?(t.expirationTime=t.childExpirationTime=1,t=null):(Mt(e,t,m,n),t=t.child),t;case 6:return e===null&&Qs(t),null;case 13:return Ga(e,t,n);case 4:return Hs(t,t.stateNode.containerInfo),r=t.pendingProps,e===null?t.child=qr(t,null,r,n):Mt(e,t,r,n),t.child;case 11:return r=t.type,s=t.pendingProps,s=t.elementType===r?s:lt(r,s),Wa(e,t,r,s,n);case 7:return Mt(e,t,t.pendingProps,n),t.child;case 8:return Mt(e,t,t.pendingProps.children,n),t.child;case 12:return Mt(e,t,t.pendingProps.children,n),t.child;case 10:e:{r=t.type._context,s=t.pendingProps,m=t.memoizedProps,d=s.value;var v=t.type._context;if(Ge(At,v._currentValue),v._currentValue=d,m!==null)if(v=m.value,d=gn(v,d)?0:(typeof r._calculateChangedBits=="function"?r._calculateChangedBits(v,d):1073741823)|0,d===0){if(m.children===s.children&&!nt.current){t=Cn(e,t,n);break e}}else for(v=t.child,v!==null&&(v.return=t);v!==null;){var L=v.dependencies;if(L!==null){m=v.child;for(var S=L.firstContext;S!==null;){if(S.context===r&&S.observedBits&d){v.tag===1&&(S=On(n,null),S.tag=2,Dn(v,S)),v.expirationTime<n&&(v.expirationTime=n),S=v.alternate,S!==null&&S.expirationTime<n&&(S.expirationTime=n),_a(v.return,n),L.expirationTime<n&&(L.expirationTime=n);break}S=S.next}}else m=v.tag===10&&v.type===t.type?null:v.child;if(m!==null)m.return=v;else for(m=v;m!==null;){if(m===t){m=null;break}if(v=m.sibling,v!==null){v.return=m.return,m=v;break}m=m.return}v=m}Mt(e,t,s.children,n),t=t.child}return t;case 9:return s=t.type,d=t.pendingProps,r=d.children,Zr(t,n),s=It(s,d.unstable_observedBits),r=r(s),t.effectTag|=1,Mt(e,t,r,n),t.child;case 14:return s=t.type,d=lt(s,t.pendingProps),d=lt(s.type,d),Za(e,t,s,d,r,n);case 15:return qa(e,t,t.type,t.pendingProps,r,n);case 17:return r=t.type,s=t.pendingProps,s=t.elementType===r?s:lt(r,s),e!==null&&(e.alternate=null,t.alternate=null,t.effectTag|=2),t.tag=1,dt(r)?(e=!0,Pn(t)):e=!1,Zr(t,n),Na(t,r,s),As(t,r,s,n),Gs(null,t,r,!0,e,n);case 19:return Ja(e,t,n)}throw Error(p(156,t.tag))},"Rj");var ma=null,pa=null;function Gu(e){if(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__=="undefined")return!1;var t=__REACT_DEVTOOLS_GLOBAL_HOOK__;if(t.isDisabled||!t.supportsFiber)return!0;try{var n=t.inject(e);ma=i(function(r){try{t.onCommitFiberRoot(n,r,void 0,(r.current.effectTag&64)===64)}catch{}},"Uj"),pa=i(function(r){try{t.onCommitFiberUnmount(n,r)}catch{}},"Li")}catch{}return!0}i(Gu,"Yj");function Xu(e,t,n,r){this.tag=e,this.key=n,this.sibling=this.child=this.return=this.stateNode=this.type=this.elementType=null,this.index=0,this.ref=null,this.pendingProps=t,this.dependencies=this.memoizedState=this.updateQueue=this.memoizedProps=null,this.mode=r,this.effectTag=0,this.lastEffect=this.firstEffect=this.nextEffect=null,this.childExpirationTime=this.expirationTime=0,this.alternate=null}i(Xu,"Zj");function nn(e,t,n,r){return new Xu(e,t,n,r)}i(nn,"Sh");function ha(e){return e=e.prototype,!(!e||!e.isReactComponent)}i(ha,"bi");function Ju(e){if(typeof e=="function")return ha(e)?1:0;if(e!=null){if(e=e.$$typeof,e===eo)return 11;if(e===to)return 14}return 2}i(Ju,"Xj");function wr(e,t){var n=e.alternate;return n===null?(n=nn(e.tag,t,e.key,e.mode),n.elementType=e.elementType,n.type=e.type,n.stateNode=e.stateNode,n.alternate=e,e.alternate=n):(n.pendingProps=t,n.effectTag=0,n.nextEffect=null,n.firstEffect=null,n.lastEffect=null),n.childExpirationTime=e.childExpirationTime,n.expirationTime=e.expirationTime,n.child=e.child,n.memoizedProps=e.memoizedProps,n.memoizedState=e.memoizedState,n.updateQueue=e.updateQueue,t=e.dependencies,n.dependencies=t===null?null:{expirationTime:t.expirationTime,firstContext:t.firstContext,responders:t.responders},n.sibling=e.sibling,n.index=e.index,n.ref=e.ref,n}i(wr,"Sg");function Pl(e,t,n,r,s,d){var m=2;if(r=e,typeof e=="function")ha(e)&&(m=1);else if(typeof e=="string")m=5;else e:switch(e){case Wt:return Vn(n.children,s,d,t);case Al:m=8,s|=7;break;case Bn:m=8,s|=1;break;case Er:return e=nn(12,n,t,s|8),e.elementType=Er,e.type=Er,e.expirationTime=d,e;case kr:return e=nn(13,n,t,s),e.type=kr,e.elementType=kr,e.expirationTime=d,e;case oi:return e=nn(19,n,t,s),e.elementType=oi,e.expirationTime=d,e;default:if(typeof e=="object"&&e!==null)switch(e.$$typeof){case ni:m=10;break e;case ri:m=9;break e;case eo:m=11;break e;case to:m=14;break e;case br:m=16,r=null;break e;case Il:m=22;break e}throw Error(p(130,e==null?e:typeof e,""))}return t=nn(m,n,t,s),t.elementType=e,t.type=r,t.expirationTime=d,t}i(Pl,"Ug");function Vn(e,t,n,r){return e=nn(7,e,r,t),e.expirationTime=n,e}i(Vn,"Wg");function va(e,t,n){return e=nn(6,e,null,t),e.expirationTime=n,e}i(va,"Tg");function ga(e,t,n){return t=nn(4,e.children!==null?e.children:[],e.key,t),t.expirationTime=n,t.stateNode={containerInfo:e.containerInfo,pendingChildren:null,implementation:e.implementation},t}i(ga,"Vg");function e1(e,t,n){this.tag=t,this.current=null,this.containerInfo=e,this.pingCache=this.pendingChildren=null,this.finishedExpirationTime=0,this.finishedWork=null,this.timeoutHandle=-1,this.pendingContext=this.context=null,this.hydrate=n,this.callbackNode=null,this.callbackPriority=90,this.lastExpiredTime=this.lastPingedTime=this.nextKnownPendingLevel=this.lastSuspendedTime=this.firstSuspendedTime=this.firstPendingTime=0}i(e1,"ak");function Su(e,t){var n=e.firstSuspendedTime;return e=e.lastSuspendedTime,n!==0&&n>=t&&e<=t}i(Su,"Aj");function xr(e,t){var n=e.firstSuspendedTime,r=e.lastSuspendedTime;n<t&&(e.firstSuspendedTime=t),(r>t||n===0)&&(e.lastSuspendedTime=t),t<=e.lastPingedTime&&(e.lastPingedTime=0),t<=e.lastExpiredTime&&(e.lastExpiredTime=0)}i(xr,"xi");function Tu(e,t){t>e.firstPendingTime&&(e.firstPendingTime=t);var n=e.firstSuspendedTime;n!==0&&(t>=n?e.firstSuspendedTime=e.lastSuspendedTime=e.nextKnownPendingLevel=0:t>=e.lastSuspendedTime&&(e.lastSuspendedTime=t+1),t>e.nextKnownPendingLevel&&(e.nextKnownPendingLevel=t))}i(Tu,"yi");function ya(e,t){var n=e.lastExpiredTime;(n===0||n>t)&&(e.lastExpiredTime=t)}i(ya,"Cj");function Rl(e,t,n,r){var s=t.current,d=tn(),m=jo.suspense;d=vr(d,s,m);e:if(n){n=n._reactInternalFiber;t:{if(on(n)!==n||n.tag!==1)throw Error(p(170));var v=n;do{switch(v.tag){case 3:v=v.stateNode.context;break t;case 1:if(dt(v.type)){v=v.stateNode.__reactInternalMemoizedMergedChildContext;break t}}v=v.return}while(v!==null);throw Error(p(171))}if(n.tag===1){var L=n.type;if(dt(L)){n=Br(n,L,v);break e}}n=v}else n=Kt;return t.context===null?t.context=n:t.pendingContext=n,t=On(d,m),t.payload={element:e},r=r===void 0?null:r,r!==null&&(t.callback=r),Dn(s,t),Fn(s,d),d}i(Rl,"bk");function Ca(e){if(e=e.current,!e.child)return null;switch(e.child.tag){case 5:return e.child.stateNode;default:return e.child.stateNode}}i(Ca,"ck");function Mu(e,t){e=e.memoizedState,e!==null&&e.dehydrated!==null&&e.retryTime<t&&(e.retryTime=t)}i(Mu,"dk");function wa(e,t){Mu(e,t),(e=e.alternate)&&Mu(e,t)}i(wa,"ek");function xa(e,t,n){n=n!=null&&n.hydrate===!0;var r=new e1(e,t,n),s=nn(3,null,null,t===2?7:t===1?3:0);r.current=s,s.stateNode=r,Os(s),e[qt]=r.current,n&&t!==0&&Ql(e,e.nodeType===9?e:e.ownerDocument),this._internalRoot=r}i(xa,"fk"),xa.prototype.render=function(e){Rl(e,this._internalRoot,null,null)},xa.prototype.unmount=function(){var e=this._internalRoot,t=e.containerInfo;Rl(null,e,null,function(){t[qt]=null})};function Jo(e){return!(!e||e.nodeType!==1&&e.nodeType!==9&&e.nodeType!==11&&(e.nodeType!==8||e.nodeValue!==" react-mount-point-unstable "))}i(Jo,"gk");function t1(e,t){if(t||(t=e?e.nodeType===9?e.documentElement:e.firstChild:null,t=!(!t||t.nodeType!==1||!t.hasAttribute("data-reactroot"))),!t)for(var n;n=e.lastChild;)e.removeChild(n);return new xa(e,0,t?{hydrate:!0}:void 0)}i(t1,"hk");function Ol(e,t,n,r,s){var d=n._reactRootContainer;if(d){var m=d._internalRoot;if(typeof s=="function"){var v=s;s=i(function(){var S=Ca(m);v.call(S)},"e")}Rl(t,m,e,s)}else{if(d=n._reactRootContainer=t1(n,r),m=d._internalRoot,typeof s=="function"){var L=s;s=i(function(){var S=Ca(m);L.call(S)},"e")}Cu(function(){Rl(t,m,e,s)})}return Ca(m)}i(Ol,"ik");function n1(e,t,n){var r=3<arguments.length&&arguments[3]!==void 0?arguments[3]:null;return{$$typeof:zn,key:r==null?null:""+r,children:e,containerInfo:t,implementation:n}}i(n1,"jk"),ao=i(function(e){if(e.tag===13){var t=_e(tn(),150,100);Fn(e,t),wa(e,t)}},"wc"),Yn=i(function(e){e.tag===13&&(Fn(e,3),wa(e,3))},"xc"),_i=i(function(e){if(e.tag===13){var t=tn();t=vr(t,e,null),Fn(e,t),wa(e,t)}},"yc"),fe=i(function(e,t,n){switch(t){case"input":if(ai(e,n),t=n.name,n.type==="radio"&&t!=null){for(n=e;n.parentNode;)n=n.parentNode;for(n=n.querySelectorAll("input[name="+JSON.stringify(""+t)+'][type="radio"]'),t=0;t<n.length;t++){var r=n[t];if(r!==e&&r.form===e.form){var s=No(r);if(!s)throw Error(p(90));li(r),ai(r,s)}}}break;case"textarea":mi(e,n);break;case"select":t=n.value,t!=null&&Wn(e,!!n.multiple,t,!1)}},"za"),qe=yu,at=i(function(e,t,n,r,s){var d=Ee;Ee|=4;try{return Te(98,e.bind(null,t,n,r,s))}finally{Ee=d,Ee===mt&&Ye()}},"Ga"),gt=i(function(){(Ee&(1|Ut|en))===mt&&(ju(),Gr())},"Ha"),xe=i(function(e,t){var n=Ee;Ee|=2;try{return e(t)}finally{Ee=n,Ee===mt&&Ye()}},"Ia");function Nu(e,t){var n=2<arguments.length&&arguments[2]!==void 0?arguments[2]:null;if(!Jo(t))throw Error(p(200));return n1(e,t,null,n)}i(Nu,"kk");var r1={Events:[rr,hn,No,G,O,Sn,function(e){Ei(e,ns)},Ve,re,fn,Sr,Gr,{current:!1}]};(function(e){var t=e.findFiberByHostInstance;return Gu(T({},e,{overrideHookState:null,overrideProps:null,setSuspenseHandler:null,scheduleUpdate:null,currentDispatcherRef:kt.ReactCurrentDispatcher,findHostInstanceByFiber:i(function(n){return n=jl(n),n===null?null:n.stateNode},"findHostInstanceByFiber"),findFiberByHostInstance:i(function(n){return t?t(n):null},"findFiberByHostInstance"),findHostInstancesForRefresh:null,scheduleRefresh:null,scheduleRoot:null,setRefreshHandler:null,getCurrentFiber:null}))})({findFiberByHostInstance:nr,bundleType:0,version:"16.14.0",rendererPackageName:"react-dom"}),K=r1,K=Nu,K=i(function(e){if(e==null)return null;if(e.nodeType===1)return e;var t=e._reactInternalFiber;if(t===void 0)throw typeof e.render=="function"?Error(p(188)):Error(p(268,Object.keys(e)));return e=jl(t),e=e===null?null:e.stateNode,e},"__webpack_unused_export__"),K=i(function(e,t){if((Ee&(Ut|en))!==mt)throw Error(p(187));var n=Ee;Ee|=1;try{return Te(99,e.bind(null,t))}finally{Ee=n,Ye()}},"__webpack_unused_export__"),K=i(function(e,t,n){if(!Jo(t))throw Error(p(200));return Ol(null,e,t,!0,n)},"__webpack_unused_export__"),_.render=function(e,t,n){if(!Jo(t))throw Error(p(200));return Ol(null,e,t,!1,n)},K=i(function(e){if(!Jo(e))throw Error(p(40));return e._reactRootContainer?(Cu(function(){Ol(null,null,e,!1,function(){e._reactRootContainer=null,e[qt]=null})}),!0):!1},"__webpack_unused_export__"),K=yu,K=i(function(e,t){return Nu(e,t,2<arguments.length&&arguments[2]!==void 0?arguments[2]:null)},"__webpack_unused_export__"),K=i(function(e,t,n,r){if(!Jo(n))throw Error(p(200));if(e==null||e._reactInternalFiber===void 0)throw Error(p(38));return Ol(e,t,n,!1,r)},"__webpack_unused_export__"),K="16.14.0"},40961:(b,_,B)=>{"use strict";function K(){if(!(typeof __REACT_DEVTOOLS_GLOBAL_HOOK__=="undefined"||typeof __REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE!="function"))try{__REACT_DEVTOOLS_GLOBAL_HOOK__.checkDCE(K)}catch(V){console.error(V)}}i(K,"checkDCE"),K(),b.exports=B(22551)},15287:(b,_,B)=>{"use strict";/** @license React v16.14.0
 * react.production.min.js
 *
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */var K=B(45228),V=typeof Symbol=="function"&&Symbol.for,T=V?Symbol.for("react.element"):60103,g=V?Symbol.for("react.portal"):60106,p=V?Symbol.for("react.fragment"):60107,D=V?Symbol.for("react.strict_mode"):60108,A=V?Symbol.for("react.profiler"):60114,$=V?Symbol.for("react.provider"):60109,H=V?Symbol.for("react.context"):60110,X=V?Symbol.for("react.forward_ref"):60112,Y=V?Symbol.for("react.suspense"):60113,Oe=V?Symbol.for("react.memo"):60115,He=V?Symbol.for("react.lazy"):60116,de=typeof Symbol=="function"&&Symbol.iterator;function De(w){for(var P="https://reactjs.org/docs/error-decoder.html?invariant="+w,he=1;he<arguments.length;he++)P+="&args[]="+encodeURIComponent(arguments[he]);return"Minified React error #"+w+"; visit "+P+" for the full message or use the non-minified dev environment for full errors and additional helpful warnings."}i(De,"C");var tt={isMounted:i(function(){return!1},"isMounted"),enqueueForceUpdate:i(function(){},"enqueueForceUpdate"),enqueueReplaceState:i(function(){},"enqueueReplaceState"),enqueueSetState:i(function(){},"enqueueSetState")},j={};function N(w,P,he){this.props=w,this.context=P,this.refs=j,this.updater=he||tt}i(N,"F"),N.prototype.isReactComponent={},N.prototype.setState=function(w,P){if(typeof w!="object"&&typeof w!="function"&&w!=null)throw Error(De(85));this.updater.enqueueSetState(this,w,P,"setState")},N.prototype.forceUpdate=function(w){this.updater.enqueueForceUpdate(this,w,"forceUpdate")};function l(){}i(l,"G"),l.prototype=N.prototype;function oe(w,P,he){this.props=w,this.context=P,this.refs=j,this.updater=he||tt}i(oe,"H");var q=oe.prototype=new l;q.constructor=oe,K(q,N.prototype),q.isPureReactComponent=!0;var Z={current:null},O=Object.prototype.hasOwnProperty,I={key:!0,ref:!0,__self:!0,__source:!0};function ne(w,P,he){var ke,be={},$e=null,xt=null;if(P!=null)for(ke in P.ref!==void 0&&(xt=P.ref),P.key!==void 0&&($e=""+P.key),P)O.call(P,ke)&&!I.hasOwnProperty(ke)&&(be[ke]=P[ke]);var Le=arguments.length-2;if(Le===1)be.children=he;else if(1<Le){for(var ge=Array(Le),Ne=0;Ne<Le;Ne++)ge[Ne]=arguments[Ne+2];be.children=ge}if(w&&w.defaultProps)for(ke in Le=w.defaultProps,Le)be[ke]===void 0&&(be[ke]=Le[ke]);return{$$typeof:T,type:w,key:$e,ref:xt,props:be,_owner:Z.current}}i(ne,"M");function G(w,P){return{$$typeof:T,type:w.type,key:P,ref:w.ref,props:w.props,_owner:w._owner}}i(G,"N");function se(w){return typeof w=="object"&&w!==null&&w.$$typeof===T}i(se,"O");function fe(w){var P={"=":"=0",":":"=2"};return"$"+(""+w).replace(/[=:]/g,function(he){return P[he]})}i(fe,"escape");var pe=/\/+/g,ve=[];function Ae(w,P,he,ke){if(ve.length){var be=ve.pop();return be.result=w,be.keyPrefix=P,be.func=he,be.context=ke,be.count=0,be}return{result:w,keyPrefix:P,func:he,context:ke,count:0}}i(Ae,"R");function Ve(w){w.result=null,w.keyPrefix=null,w.func=null,w.context=null,w.count=0,10>ve.length&&ve.push(w)}i(Ve,"S");function re(w,P,he,ke){var be=typeof w;(be==="undefined"||be==="boolean")&&(w=null);var $e=!1;if(w===null)$e=!0;else switch(be){case"string":case"number":$e=!0;break;case"object":switch(w.$$typeof){case T:case g:$e=!0}}if($e)return he(ke,w,P===""?"."+at(w,0):P),1;if($e=0,P=P===""?".":P+":",Array.isArray(w))for(var xt=0;xt<w.length;xt++){be=w[xt];var Le=P+at(be,xt);$e+=re(be,Le,he,ke)}else if(w===null||typeof w!="object"?Le=null:(Le=de&&w[de]||w["@@iterator"],Le=typeof Le=="function"?Le:null),typeof Le=="function")for(w=Le.call(w),xt=0;!(be=w.next()).done;)be=be.value,Le=P+at(be,xt++),$e+=re(be,Le,he,ke);else if(be==="object")throw he=""+w,Error(De(31,he==="[object Object]"?"object with keys {"+Object.keys(w).join(", ")+"}":he,""));return $e}i(re,"T");function qe(w,P,he){return w==null?0:re(w,"",P,he)}i(qe,"V");function at(w,P){return typeof w=="object"&&w!==null&&w.key!=null?fe(w.key):P.toString(36)}i(at,"U");function gt(w,P){w.func.call(w.context,P,w.count++)}i(gt,"W");function xe(w,P,he){var ke=w.result,be=w.keyPrefix;w=w.func.call(w.context,P,w.count++),Array.isArray(w)?Ue(w,ke,he,function($e){return $e}):w!=null&&(se(w)&&(w=G(w,be+(!w.key||P&&P.key===w.key?"":(""+w.key).replace(pe,"$&/")+"/")+he)),ke.push(w))}i(xe,"aa");function Ue(w,P,he,ke,be){var $e="";he!=null&&($e=(""+he).replace(pe,"$&/")+"/"),P=Ae(P,$e,ke,be),qe(w,xe,P),Ve(P)}i(Ue,"X");var z={current:null};function Q(){var w=z.current;if(w===null)throw Error(De(321));return w}i(Q,"Z");var ue={ReactCurrentDispatcher:z,ReactCurrentBatchConfig:{suspense:null},ReactCurrentOwner:Z,IsSomeRendererActing:{current:!1},assign:K};_.Children={map:i(function(w,P,he){if(w==null)return w;var ke=[];return Ue(w,ke,null,P,he),ke},"map"),forEach:i(function(w,P,he){if(w==null)return w;P=Ae(null,null,P,he),qe(w,gt,P),Ve(P)},"forEach"),count:i(function(w){return qe(w,function(){return null},null)},"count"),toArray:i(function(w){var P=[];return Ue(w,P,null,function(he){return he}),P},"toArray"),only:i(function(w){if(!se(w))throw Error(De(143));return w},"only")},_.Component=N,_.Fragment=p,_.Profiler=A,_.PureComponent=oe,_.StrictMode=D,_.Suspense=Y,_.__SECRET_INTERNALS_DO_NOT_USE_OR_YOU_WILL_BE_FIRED=ue,_.cloneElement=function(w,P,he){if(w==null)throw Error(De(267,w));var ke=K({},w.props),be=w.key,$e=w.ref,xt=w._owner;if(P!=null){if(P.ref!==void 0&&($e=P.ref,xt=Z.current),P.key!==void 0&&(be=""+P.key),w.type&&w.type.defaultProps)var Le=w.type.defaultProps;for(ge in P)O.call(P,ge)&&!I.hasOwnProperty(ge)&&(ke[ge]=P[ge]===void 0&&Le!==void 0?Le[ge]:P[ge])}var ge=arguments.length-2;if(ge===1)ke.children=he;else if(1<ge){Le=Array(ge);for(var Ne=0;Ne<ge;Ne++)Le[Ne]=arguments[Ne+2];ke.children=Le}return{$$typeof:T,type:w.type,key:be,ref:$e,props:ke,_owner:xt}},_.createContext=function(w,P){return P===void 0&&(P=null),w={$$typeof:H,_calculateChangedBits:P,_currentValue:w,_currentValue2:w,_threadCount:0,Provider:null,Consumer:null},w.Provider={$$typeof:$,_context:w},w.Consumer=w},_.createElement=ne,_.createFactory=function(w){var P=ne.bind(null,w);return P.type=w,P},_.createRef=function(){return{current:null}},_.forwardRef=function(w){return{$$typeof:X,render:w}},_.isValidElement=se,_.lazy=function(w){return{$$typeof:He,_ctor:w,_status:-1,_result:null}},_.memo=function(w,P){return{$$typeof:Oe,type:w,compare:P===void 0?null:P}},_.useCallback=function(w,P){return Q().useCallback(w,P)},_.useContext=function(w,P){return Q().useContext(w,P)},_.useDebugValue=function(){},_.useEffect=function(w,P){return Q().useEffect(w,P)},_.useImperativeHandle=function(w,P,he){return Q().useImperativeHandle(w,P,he)},_.useLayoutEffect=function(w,P){return Q().useLayoutEffect(w,P)},_.useMemo=function(w,P){return Q().useMemo(w,P)},_.useReducer=function(w,P,he){return Q().useReducer(w,P,he)},_.useRef=function(w){return Q().useRef(w)},_.useState=function(w){return Q().useState(w)},_.version="16.14.0"},96540:(b,_,B)=>{"use strict";b.exports=B(15287)},7463:(b,_)=>{"use strict";/** @license React v0.19.1
 * scheduler.production.min.js
 *
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */var B,K,V,T,g;if(typeof window=="undefined"||typeof MessageChannel!="function"){var p=null,D=null,A=i(function(){if(p!==null)try{var z=_.unstable_now();p(!0,z),p=null}catch(Q){throw setTimeout(A,0),Q}},"t"),$=Date.now();_.unstable_now=function(){return Date.now()-$},B=i(function(z){p!==null?setTimeout(B,0,z):(p=z,setTimeout(A,0))},"f"),K=i(function(z,Q){D=setTimeout(z,Q)},"g"),V=i(function(){clearTimeout(D)},"h"),T=i(function(){return!1},"k"),g=_.unstable_forceFrameRate=function(){}}else{var H=window.performance,X=window.Date,Y=window.setTimeout,Oe=window.clearTimeout;if(typeof console!="undefined"){var He=window.cancelAnimationFrame;typeof window.requestAnimationFrame!="function"&&console.error("This browser doesn't support requestAnimationFrame. Make sure that you load a polyfill in older browsers. https://fb.me/react-polyfills"),typeof He!="function"&&console.error("This browser doesn't support cancelAnimationFrame. Make sure that you load a polyfill in older browsers. https://fb.me/react-polyfills")}if(typeof H=="object"&&typeof H.now=="function")_.unstable_now=function(){return H.now()};else{var de=X.now();_.unstable_now=function(){return X.now()-de}}var De=!1,tt=null,j=-1,N=5,l=0;T=i(function(){return _.unstable_now()>=l},"k"),g=i(function(){},"l"),_.unstable_forceFrameRate=function(z){0>z||125<z?console.error("forceFrameRate takes a positive int between 0 and 125, forcing framerates higher than 125 fps is not unsupported"):N=0<z?Math.floor(1e3/z):5};var oe=new MessageChannel,q=oe.port2;oe.port1.onmessage=function(){if(tt!==null){var z=_.unstable_now();l=z+N;try{tt(!0,z)?q.postMessage(null):(De=!1,tt=null)}catch(Q){throw q.postMessage(null),Q}}else De=!1},B=i(function(z){tt=z,De||(De=!0,q.postMessage(null))},"f"),K=i(function(z,Q){j=Y(function(){z(_.unstable_now())},Q)},"g"),V=i(function(){Oe(j),j=-1},"h")}function Z(z,Q){var ue=z.length;z.push(Q);e:for(;;){var w=ue-1>>>1,P=z[w];if(P!==void 0&&0<ne(P,Q))z[w]=Q,z[ue]=P,ue=w;else break e}}i(Z,"J");function O(z){return z=z[0],z===void 0?null:z}i(O,"L");function I(z){var Q=z[0];if(Q!==void 0){var ue=z.pop();if(ue!==Q){z[0]=ue;e:for(var w=0,P=z.length;w<P;){var he=2*(w+1)-1,ke=z[he],be=he+1,$e=z[be];if(ke!==void 0&&0>ne(ke,ue))$e!==void 0&&0>ne($e,ke)?(z[w]=$e,z[be]=ue,w=be):(z[w]=ke,z[he]=ue,w=he);else if($e!==void 0&&0>ne($e,ue))z[w]=$e,z[be]=ue,w=be;else break e}}return Q}return null}i(I,"M");function ne(z,Q){var ue=z.sortIndex-Q.sortIndex;return ue!==0?ue:z.id-Q.id}i(ne,"K");var G=[],se=[],fe=1,pe=null,ve=3,Ae=!1,Ve=!1,re=!1;function qe(z){for(var Q=O(se);Q!==null;){if(Q.callback===null)I(se);else if(Q.startTime<=z)I(se),Q.sortIndex=Q.expirationTime,Z(G,Q);else break;Q=O(se)}}i(qe,"V");function at(z){if(re=!1,qe(z),!Ve)if(O(G)!==null)Ve=!0,B(gt);else{var Q=O(se);Q!==null&&K(at,Q.startTime-z)}}i(at,"W");function gt(z,Q){Ve=!1,re&&(re=!1,V()),Ae=!0;var ue=ve;try{for(qe(Q),pe=O(G);pe!==null&&(!(pe.expirationTime>Q)||z&&!T());){var w=pe.callback;if(w!==null){pe.callback=null,ve=pe.priorityLevel;var P=w(pe.expirationTime<=Q);Q=_.unstable_now(),typeof P=="function"?pe.callback=P:pe===O(G)&&I(G),qe(Q)}else I(G);pe=O(G)}if(pe!==null)var he=!0;else{var ke=O(se);ke!==null&&K(at,ke.startTime-Q),he=!1}return he}finally{pe=null,ve=ue,Ae=!1}}i(gt,"X");function xe(z){switch(z){case 1:return-1;case 2:return 250;case 5:return 1073741823;case 4:return 1e4;default:return 5e3}}i(xe,"Y");var Ue=g;_.unstable_IdlePriority=5,_.unstable_ImmediatePriority=1,_.unstable_LowPriority=4,_.unstable_NormalPriority=3,_.unstable_Profiling=null,_.unstable_UserBlockingPriority=2,_.unstable_cancelCallback=function(z){z.callback=null},_.unstable_continueExecution=function(){Ve||Ae||(Ve=!0,B(gt))},_.unstable_getCurrentPriorityLevel=function(){return ve},_.unstable_getFirstCallbackNode=function(){return O(G)},_.unstable_next=function(z){switch(ve){case 1:case 2:case 3:var Q=3;break;default:Q=ve}var ue=ve;ve=Q;try{return z()}finally{ve=ue}},_.unstable_pauseExecution=function(){},_.unstable_requestPaint=Ue,_.unstable_runWithPriority=function(z,Q){switch(z){case 1:case 2:case 3:case 4:case 5:break;default:z=3}var ue=ve;ve=z;try{return Q()}finally{ve=ue}},_.unstable_scheduleCallback=function(z,Q,ue){var w=_.unstable_now();if(typeof ue=="object"&&ue!==null){var P=ue.delay;P=typeof P=="number"&&0<P?w+P:w,ue=typeof ue.timeout=="number"?ue.timeout:xe(z)}else ue=xe(z),P=w;return ue=P+ue,z={id:fe++,callback:Q,priorityLevel:z,startTime:P,expirationTime:ue,sortIndex:-1},P>w?(z.sortIndex=P,Z(se,z),O(G)===null&&z===O(se)&&(re?V():re=!0,K(at,P-w))):(z.sortIndex=ue,Z(G,z),Ve||Ae||(Ve=!0,B(gt))),z},_.unstable_shouldYield=function(){var z=_.unstable_now();qe(z);var Q=O(G);return Q!==pe&&pe!==null&&Q!==null&&Q.callback!==null&&Q.startTime<=z&&Q.expirationTime<pe.expirationTime||T()},_.unstable_wrapCallback=function(z){var Q=ve;return function(){var ue=ve;ve=Q;try{return z.apply(this,arguments)}finally{ve=ue}}}},69982:(b,_,B)=>{"use strict";b.exports=B(7463)},85072:b=>{"use strict";var _=[];function B(T){for(var g=-1,p=0;p<_.length;p++)if(_[p].identifier===T){g=p;break}return g}i(B,"getIndexByIdentifier");function K(T,g){for(var p={},D=[],A=0;A<T.length;A++){var $=T[A],H=g.base?$[0]+g.base:$[0],X=p[H]||0,Y="".concat(H," ").concat(X);p[H]=X+1;var Oe=B(Y),He={css:$[1],media:$[2],sourceMap:$[3],supports:$[4],layer:$[5]};if(Oe!==-1)_[Oe].references++,_[Oe].updater(He);else{var de=V(He,g);g.byIndex=A,_.splice(A,0,{identifier:Y,updater:de,references:1})}D.push(Y)}return D}i(K,"modulesToDom");function V(T,g){var p=g.domAPI(g);p.update(T);var D=i(function($){if($){if($.css===T.css&&$.media===T.media&&$.sourceMap===T.sourceMap&&$.supports===T.supports&&$.layer===T.layer)return;p.update(T=$)}else p.remove()},"updater");return D}i(V,"addElementStyle"),b.exports=function(T,g){g=g||{},T=T||[];var p=K(T,g);return i(function(A){A=A||[];for(var $=0;$<p.length;$++){var H=p[$],X=B(H);_[X].references--}for(var Y=K(A,g),Oe=0;Oe<p.length;Oe++){var He=p[Oe],de=B(He);_[de].references===0&&(_[de].updater(),_.splice(de,1))}p=Y},"update")}},77659:b=>{"use strict";var _={};function B(V){if(typeof _[V]=="undefined"){var T=document.querySelector(V);if(window.HTMLIFrameElement&&T instanceof window.HTMLIFrameElement)try{T=T.contentDocument.head}catch{T=null}_[V]=T}return _[V]}i(B,"getTarget");function K(V,T){var g=B(V);if(!g)throw new Error("Couldn't find a style target. This probably means that the value for the 'insert' parameter is invalid.");g.appendChild(T)}i(K,"insertBySelector"),b.exports=K},10540:b=>{"use strict";function _(B){var K=document.createElement("style");return B.setAttributes(K,B.attributes),B.insert(K,B.options),K}i(_,"insertStyleElement"),b.exports=_},55056:(b,_,B)=>{"use strict";function K(V){var T=B.nc;T&&V.setAttribute("nonce",T)}i(K,"setAttributesWithoutAttributes"),b.exports=K},97825:b=>{"use strict";function _(V,T,g){var p="";g.supports&&(p+="@supports (".concat(g.supports,") {")),g.media&&(p+="@media ".concat(g.media," {"));var D=typeof g.layer!="undefined";D&&(p+="@layer".concat(g.layer.length>0?" ".concat(g.layer):""," {")),p+=g.css,D&&(p+="}"),g.media&&(p+="}"),g.supports&&(p+="}");var A=g.sourceMap;A&&typeof btoa!="undefined"&&(p+=`
/*# sourceMappingURL=data:application/json;base64,`.concat(btoa(unescape(encodeURIComponent(JSON.stringify(A))))," */")),T.styleTagTransform(p,V,T.options)}i(_,"apply");function B(V){if(V.parentNode===null)return!1;V.parentNode.removeChild(V)}i(B,"removeStyleElement");function K(V){if(typeof document=="undefined")return{update:i(function(){},"update"),remove:i(function(){},"remove")};var T=V.insertStyleElement(V);return{update:i(function(p){_(T,V,p)},"update"),remove:i(function(){B(T)},"remove")}}i(K,"domAPI"),b.exports=K},41113:b=>{"use strict";function _(B,K){if(K.styleSheet)K.styleSheet.cssText=B;else{for(;K.firstChild;)K.removeChild(K.firstChild);K.appendChild(document.createTextNode(B))}}i(_,"styleTagTransform"),b.exports=_},61440:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M14.12 13.9725L15 12.5L9.37924 2H7.61921L1.99847 12.5L2.87849 13.9725H14.12ZM2.87849 12.9725L8.49922 2.47249L14.12 12.9725H2.87849ZM7.98949 6H8.98799V10H7.98949V6ZM7.98949 11H8.98799V12H7.98949V11Z" fill="#C5C5C5"></path></svg>'},34439:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><g clip-path="url(#clip0_818_123307)"><path d="M16 7.99201C16 3.58042 12.416 0 8 0C3.584 0 0 3.58042 0 7.99201C0 10.4216 1.104 12.6114 2.832 14.0819C2.848 14.0979 2.864 14.0979 2.864 14.1139C3.008 14.2258 3.152 14.3377 3.312 14.4496C3.392 14.4975 3.456 14.5614 3.536 14.6254C4.816 15.4885 6.352 16 8.016 16C9.68 16 11.216 15.4885 12.496 14.6254C12.576 14.5774 12.64 14.5135 12.72 14.4655C12.864 14.3536 13.024 14.2418 13.168 14.1299C13.184 14.1139 13.2 14.1139 13.2 14.0979C14.896 12.6114 16 10.4216 16 7.99201ZM8 14.993C6.496 14.993 5.12 14.5135 3.984 13.7143C4 13.5864 4.032 13.4585 4.064 13.3307C4.16 12.979 4.304 12.6434 4.48 12.3397C4.656 12.036 4.864 11.7642 5.12 11.5245C5.36 11.2847 5.648 11.0609 5.936 10.8851C6.24 10.7093 6.56 10.5814 6.912 10.4855C7.264 10.3896 7.632 10.3417 8 10.3417C8.592 10.3417 9.136 10.4535 9.632 10.6613C10.128 10.8691 10.56 11.1568 10.928 11.5085C11.296 11.8761 11.584 12.3077 11.792 12.8032C11.904 13.0909 11.984 13.3946 12.032 13.7143C10.88 14.5135 9.504 14.993 8 14.993ZM5.552 7.59241C5.408 7.27273 5.344 6.92108 5.344 6.56943C5.344 6.21778 5.408 5.86613 5.552 5.54645C5.696 5.22677 5.888 4.93906 6.128 4.6993C6.368 4.45954 6.656 4.26773 6.976 4.12388C7.296 3.98002 7.648 3.91608 8 3.91608C8.368 3.91608 8.704 3.98002 9.024 4.12388C9.344 4.26773 9.632 4.45954 9.872 4.6993C10.112 4.93906 10.304 5.22677 10.448 5.54645C10.592 5.86613 10.656 6.21778 10.656 6.56943C10.656 6.93706 10.592 7.27273 10.448 7.59241C10.304 7.91209 10.112 8.1998 9.872 8.43956C9.632 8.67932 9.344 8.87113 9.024 9.01499C8.384 9.28671 7.6 9.28671 6.96 9.01499C6.64 8.87113 6.352 8.67932 6.112 8.43956C5.872 8.1998 5.68 7.91209 5.552 7.59241ZM12.976 12.8991C12.976 12.8671 12.96 12.8511 12.96 12.8192C12.8 12.3237 12.576 11.8442 12.272 11.4126C11.968 10.981 11.616 10.5974 11.184 10.2777C10.864 10.038 10.512 9.83017 10.144 9.67033C10.32 9.55844 10.48 9.41459 10.608 9.28671C10.848 9.04695 11.056 8.79121 11.232 8.5035C11.408 8.21578 11.536 7.91209 11.632 7.57642C11.728 7.24076 11.76 6.90509 11.76 6.56943C11.76 6.04196 11.664 5.54645 11.472 5.0989C11.28 4.65135 11.008 4.25175 10.656 3.9001C10.32 3.56444 9.904 3.29271 9.456 3.1009C9.008 2.90909 8.512 2.81319 7.984 2.81319C7.456 2.81319 6.96 2.90909 6.512 3.1009C6.064 3.29271 5.648 3.56444 5.312 3.91608C4.976 4.25175 4.704 4.66733 4.512 5.11489C4.32 5.56244 4.224 6.05794 4.224 6.58541C4.224 6.93706 4.272 7.27273 4.368 7.59241C4.464 7.92807 4.592 8.23177 4.768 8.51948C4.928 8.80719 5.152 9.06294 5.392 9.3027C5.536 9.44655 5.696 9.57443 5.872 9.68631C5.488 9.86214 5.136 10.0699 4.832 10.3097C4.416 10.6294 4.048 11.013 3.744 11.4286C3.44 11.8601 3.216 12.3237 3.056 12.8352C3.04 12.8671 3.04 12.8991 3.04 12.9151C1.776 11.6364 0.992 9.91009 0.992 7.99201C0.992 4.13986 4.144 0.991009 8 0.991009C11.856 0.991009 15.008 4.13986 15.008 7.99201C15.008 9.91009 14.224 11.6364 12.976 12.8991Z" fill="#C5C5C5"></path></g><defs><clipPath id="clip0_818_123307"><rect width="16" height="16" fill="white"></rect></clipPath></defs></svg>'},34894:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M13.78 4.22a.75.75 0 010 1.06l-7.25 7.25a.75.75 0 01-1.06 0L2.22 9.28a.75.75 0 011.06-1.06L6 10.94l6.72-6.72a.75.75 0 011.06 0z" fill="#C5C5C5"></path></svg>'},30407:b=>{b.exports='<svg viewBox="0 -2 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M7.97612 10.0719L12.3334 5.7146L12.9521 6.33332L8.28548 11L7.66676 11L3.0001 6.33332L3.61882 5.7146L7.97612 10.0719Z" fill="#C5C5C5"></path></svg>'},10650:b=>{b.exports='<svg viewBox="0 0 16 16" fill="currentColor" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M7.97612 10.0719L12.3334 5.7146L12.9521 6.33332L8.28548 11L7.66676 11L3.0001 6.33332L3.61882 5.7146L7.97612 10.0719Z"></path></svg>'},85130:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M7.99998 8.70711L11.6464 12.3536L12.3535 11.6464L8.70708 8L12.3535 4.35355L11.6464 3.64645L7.99998 7.29289L4.35353 3.64645L3.64642 4.35355L7.29287 8L3.64642 11.6464L4.35353 12.3536L7.99998 8.70711Z" fill="#C5C5C5"></path></svg>'},2301:b=>{b.exports='<svg viewBox="0 0 16 16" version="1.1" aria-hidden="true"><path fill-rule="evenodd" d="M14 1H2c-.55 0-1 .45-1 1v8c0 .55.45 1 1 1h2v3.5L7.5 11H14c.55 0 1-.45 1-1V2c0-.55-.45-1-1-1zm0 9H7l-2 2v-2H2V2h12v8z"></path></svg>'},5771:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M7.52 0H8.48V4.05333C9.47556 4.16 10.3111 4.58667 10.9867 5.33333C11.6622 6.08 12 6.96889 12 8C12 9.03111 11.6622 9.92 10.9867 10.6667C10.3111 11.4133 9.47556 11.84 8.48 11.9467V16H7.52V11.9467C6.52444 11.84 5.68889 11.4133 5.01333 10.6667C4.33778 9.92 4 9.03111 4 8C4 6.96889 4.33778 6.08 5.01333 5.33333C5.68889 4.58667 6.52444 4.16 7.52 4.05333V0ZM8 10.6133C8.71111 10.6133 9.31556 10.3644 9.81333 9.86667C10.3467 9.33333 10.6133 8.71111 10.6133 8C10.6133 7.28889 10.3467 6.68444 9.81333 6.18667C9.31556 5.65333 8.71111 5.38667 8 5.38667C7.28889 5.38667 6.66667 5.65333 6.13333 6.18667C5.63556 6.68444 5.38667 7.28889 5.38667 8C5.38667 8.71111 5.63556 9.33333 6.13333 9.86667C6.66667 10.3644 7.28889 10.6133 8 10.6133Z" fill="#A0A0A0"></path></svg>'},94339:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M13.807 2.265C13.228 1.532 12.313 1.141 11.083 1.004C9.877 0.870002 8.821 1.038 8.139 1.769C8.09 1.822 8.043 1.877 8 1.933C7.957 1.877 7.91 1.822 7.861 1.769C7.179 1.038 6.123 0.870002 4.917 1.004C3.687 1.141 2.772 1.532 2.193 2.265C1.628 2.981 1.5 3.879 1.5 4.75C1.5 5.322 1.553 5.897 1.754 6.405L1.586 7.243L1.52 7.276C0.588 7.742 0 8.694 0 9.736V11C0 11.24 0.086 11.438 0.156 11.567C0.231 11.704 0.325 11.828 0.415 11.933C0.595 12.143 0.819 12.346 1.02 12.513C1.225 12.684 1.427 12.836 1.577 12.943C1.816 13.116 2.062 13.275 2.318 13.423C2.625 13.6 3.066 13.832 3.614 14.065C4.391 14.395 5.404 14.722 6.553 14.887C6.203 14.377 5.931 13.809 5.751 13.202C5.173 13.055 4.645 12.873 4.201 12.684C3.717 12.479 3.331 12.274 3.067 12.123L3.002 12.085V7.824L3.025 7.709C3.515 7.919 4.1 8 4.752 8C5.898 8 6.812 7.672 7.462 7.009C7.681 6.785 7.859 6.535 8.002 6.266C8.049 6.354 8.106 6.436 8.16 6.52C8.579 6.238 9.038 6.013 9.522 5.843C9.26 5.52 9.077 5.057 8.996 4.407C8.879 3.471 9.034 3.011 9.238 2.793C9.431 2.586 9.875 2.379 10.919 2.495C11.939 2.608 12.398 2.899 12.632 3.195C12.879 3.508 13.002 3.984 13.002 4.75C13.002 5.158 12.967 5.453 12.909 5.674C13.398 5.792 13.865 5.967 14.3 6.197C14.443 5.741 14.502 5.248 14.502 4.75C14.502 3.879 14.374 2.981 13.809 2.265H13.807ZM7.006 4.407C6.915 5.133 6.704 5.637 6.388 5.959C6.089 6.264 5.604 6.5 4.75 6.5C3.828 6.5 3.47 6.301 3.308 6.12C3.129 5.92 3 5.542 3 4.75C3 3.984 3.123 3.508 3.37 3.195C3.604 2.899 4.063 2.609 5.083 2.495C6.127 2.379 6.571 2.586 6.764 2.793C6.968 3.011 7.123 3.471 7.006 4.407Z" fill="currentColor"></path><path d="M11.5 7C9.015 7 7 9.015 7 11.5C7 13.985 9.015 16 11.5 16C13.985 16 16 13.985 16 11.5C16 9.015 13.985 7 11.5 7ZM13.854 13.146L13.147 13.853L11.501 12.207L9.855 13.853L9.148 13.146L10.794 11.5L9.148 9.854L9.855 9.147L11.501 10.793L13.147 9.147L13.854 9.854L12.208 11.5L13.854 13.146Z" fill="var(--vscode-list-errorForeground, currentColor)"></path></svg>'},58726:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M13.807 2.265C13.228 1.532 12.313 1.141 11.083 1.004C9.877 0.870002 8.821 1.038 8.139 1.769C8.09 1.822 8.043 1.877 8 1.933C7.957 1.877 7.91 1.822 7.861 1.769C7.179 1.038 6.123 0.870002 4.917 1.004C3.687 1.141 2.772 1.532 2.193 2.265C1.628 2.981 1.5 3.879 1.5 4.75C1.5 5.322 1.553 5.897 1.754 6.405L1.586 7.243L1.52 7.276C0.588 7.742 0 8.694 0 9.736V11C0 11.24 0.086 11.438 0.156 11.567C0.231 11.704 0.325 11.828 0.415 11.933C0.595 12.143 0.819 12.346 1.02 12.513C1.225 12.684 1.427 12.836 1.577 12.943C1.816 13.116 2.062 13.275 2.318 13.423C2.625 13.6 3.066 13.832 3.614 14.065C4.391 14.395 5.404 14.722 6.553 14.887C6.203 14.377 5.931 13.809 5.751 13.202C5.173 13.055 4.645 12.873 4.201 12.684C3.717 12.479 3.331 12.274 3.067 12.123L3.002 12.085V7.824L3.025 7.709C3.515 7.919 4.1 8 4.752 8C5.898 8 6.812 7.672 7.462 7.009C7.681 6.785 7.859 6.535 8.002 6.266C8.049 6.354 8.106 6.436 8.16 6.52C8.579 6.238 9.038 6.013 9.522 5.843C9.26 5.52 9.077 5.057 8.996 4.407C8.879 3.471 9.034 3.011 9.238 2.793C9.431 2.586 9.875 2.379 10.919 2.495C11.939 2.608 12.398 2.899 12.632 3.195C12.879 3.508 13.002 3.984 13.002 4.75C13.002 5.158 12.967 5.453 12.909 5.674C13.398 5.792 13.865 5.967 14.3 6.197C14.443 5.741 14.502 5.248 14.502 4.75C14.502 3.879 14.374 2.981 13.809 2.265H13.807ZM7.006 4.407C6.915 5.133 6.704 5.637 6.388 5.959C6.089 6.264 5.604 6.5 4.75 6.5C3.828 6.5 3.47 6.301 3.308 6.12C3.129 5.92 3 5.542 3 4.75C3 3.984 3.123 3.508 3.37 3.195C3.604 2.899 4.063 2.609 5.083 2.495C6.127 2.379 6.571 2.586 6.764 2.793C6.968 3.011 7.123 3.471 7.006 4.407Z" fill="currentColor"></path><path d="M11.5 7C9.015 7 7 9.015 7 11.5C7 13.985 9.015 16 11.5 16C13.985 16 16 13.985 16 11.5C16 9.015 13.985 7 11.5 7ZM11.5 14.25C10.963 14.25 10.445 14.105 10 13.844V14.5H9V12.5L9.5 12H11.5V13H10.536C10.823 13.16 11.155 13.25 11.5 13.25C12.177 13.25 12.805 12.907 13.137 12.354L13.994 12.87C13.481 13.722 12.525 14.25 11.5 14.25ZM14 10.5L13.5 11H11.5V10H12.464C12.177 9.84 11.845 9.75 11.5 9.75C10.823 9.75 10.195 10.093 9.863 10.646L9.006 10.13C9.519 9.278 10.475 8.75 11.5 8.75C12.037 8.75 12.555 8.895 13 9.156V8.5H14V10.5Z" fill="var(--vscode-editorWarning-foreground, currentColor)"></path></svg>'},9336:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M13.807 2.265C13.228 1.532 12.313 1.141 11.083 1.004C9.877 0.870002 8.821 1.038 8.139 1.769C8.09 1.822 8.043 1.877 8 1.933C7.957 1.877 7.91 1.822 7.861 1.769C7.179 1.038 6.123 0.870002 4.917 1.004C3.687 1.141 2.772 1.532 2.193 2.265C1.628 2.981 1.5 3.879 1.5 4.75C1.5 5.322 1.553 5.897 1.754 6.405L1.586 7.243L1.52 7.276C0.588 7.742 0 8.694 0 9.736V11C0 11.24 0.086 11.438 0.156 11.567C0.231 11.704 0.325 11.828 0.415 11.933C0.595 12.143 0.819 12.346 1.02 12.513C1.225 12.684 1.427 12.836 1.577 12.943C1.816 13.116 2.062 13.275 2.318 13.423C2.625 13.6 3.066 13.832 3.614 14.065C4.391 14.395 5.404 14.722 6.553 14.887C6.203 14.377 5.931 13.809 5.751 13.202C5.173 13.055 4.645 12.873 4.201 12.684C3.717 12.479 3.331 12.274 3.067 12.123L3.002 12.085V7.824L3.025 7.709C3.515 7.919 4.1 8 4.752 8C5.898 8 6.812 7.672 7.462 7.009C7.681 6.785 7.859 6.535 8.002 6.266C8.049 6.354 8.106 6.436 8.16 6.52C8.579 6.238 9.038 6.013 9.522 5.843C9.26 5.52 9.077 5.057 8.996 4.407C8.879 3.471 9.034 3.011 9.238 2.793C9.431 2.586 9.875 2.379 10.919 2.495C11.939 2.608 12.398 2.899 12.632 3.195C12.879 3.508 13.002 3.984 13.002 4.75C13.002 5.158 12.967 5.453 12.909 5.674C13.398 5.792 13.865 5.967 14.3 6.197C14.443 5.741 14.502 5.248 14.502 4.75C14.502 3.879 14.374 2.981 13.809 2.265H13.807ZM7.006 4.407C6.915 5.133 6.704 5.637 6.388 5.959C6.089 6.264 5.604 6.5 4.75 6.5C3.828 6.5 3.47 6.301 3.308 6.12C3.129 5.92 3 5.542 3 4.75C3 3.984 3.123 3.508 3.37 3.195C3.604 2.899 4.063 2.609 5.083 2.495C6.127 2.379 6.571 2.586 6.764 2.793C6.968 3.011 7.123 3.471 7.006 4.407Z" fill="currentColor"></path><path d="M11.5 7C9.015 7 7 9.015 7 11.5C7 13.985 9.015 16 11.5 16C13.985 16 16 13.985 16 11.5C16 9.015 13.985 7 11.5 7ZM11.393 13.309L10.7 13.401L8.7 11.901L9.3 11.1L10.909 12.307L13.357 9.192L14.143 9.809L11.393 13.309Z" fill="var(--vscode-notebookStatusSuccessIcon-foreground, currentColor)"></path></svg>'},12158:b=>{b.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path d="M6.25 9.016C6.66421 9.016 7 9.35089 7 9.76399V11.26C7 11.6731 6.66421 12.008 6.25 12.008C5.83579 12.008 5.5 11.6731 5.5 11.26V9.76399C5.5 9.35089 5.83579 9.016 6.25 9.016Z"></path><path d="M10.5 9.76399C10.5 9.35089 10.1642 9.016 9.75 9.016C9.33579 9.016 9 9.35089 9 9.76399V11.26C9 11.6731 9.33579 12.008 9.75 12.008C10.1642 12.008 10.5 11.6731 10.5 11.26V9.76399Z"></path><path d="M7.86079 1.80482C7.91028 1.8577 7.95663 1.91232 8 1.96856C8.04337 1.91232 8.08972 1.8577 8.13921 1.80482C8.82116 1.07611 9.87702 0.90832 11.0828 1.04194C12.3131 1.17827 13.2283 1.56829 13.8072 2.29916C14.3725 3.01276 14.5 3.90895 14.5 4.77735C14.5 5.34785 14.447 5.92141 14.2459 6.428L14.4135 7.26391L14.4798 7.29699C15.4115 7.76158 16 8.71126 16 9.7501V11.0107C16 11.2495 15.9143 11.4478 15.844 11.5763C15.7691 11.7131 15.6751 11.8368 15.5851 11.9416C15.4049 12.1512 15.181 12.3534 14.9801 12.5202C14.7751 12.6907 14.5728 12.8419 14.4235 12.9494C14.1842 13.1217 13.9389 13.2807 13.6826 13.4277C13.3756 13.6038 12.9344 13.8361 12.3867 14.0679C11.2956 14.5296 9.75604 15 8 15C6.24396 15 4.70442 14.5296 3.61334 14.0679C3.06559 13.8361 2.62435 13.6038 2.31739 13.4277C2.0611 13.2807 1.81581 13.1217 1.57651 12.9494C1.42716 12.8419 1.2249 12.6907 1.01986 12.5202C0.819 12.3534 0.595113 12.1512 0.414932 11.9416C0.3249 11.8368 0.230849 11.7131 0.156031 11.5763C0.0857453 11.4478 0 11.2495 1.90735e-06 11.0107L0 9.7501C0 8.71126 0.588507 7.76158 1.52017 7.29699L1.5865 7.26391L1.75413 6.42799C1.55295 5.9214 1.5 5.34785 1.5 4.77735C1.5 3.90895 1.62745 3.01276 2.19275 2.29916C2.77172 1.56829 3.68694 1.17827 4.91718 1.04194C6.12298 0.90832 7.17884 1.07611 7.86079 1.80482ZM3.0231 7.7282L3 7.8434V12.0931C3.02086 12.1053 3.04268 12.1179 3.06543 12.131C3.32878 12.2821 3.71567 12.4861 4.19916 12.6907C5.17058 13.1017 6.50604 13.504 8 13.504C9.49396 13.504 10.8294 13.1017 11.8008 12.6907C12.2843 12.4861 12.6712 12.2821 12.9346 12.131C12.9573 12.1179 12.9791 12.1053 13 12.0931V7.8434L12.9769 7.7282C12.4867 7.93728 11.9022 8.01867 11.25 8.01867C10.1037 8.01867 9.19051 7.69201 8.54033 7.03004C8.3213 6.80703 8.14352 6.55741 8 6.28924C7.85648 6.55741 7.6787 6.80703 7.45967 7.03004C6.80949 7.69201 5.89633 8.01867 4.75 8.01867C4.09776 8.01867 3.51325 7.93728 3.0231 7.7282ZM6.76421 2.82557C6.57116 2.61928 6.12702 2.41307 5.08282 2.52878C4.06306 2.64179 3.60328 2.93176 3.36975 3.22656C3.12255 3.53861 3 4.01374 3 4.77735C3 5.56754 3.12905 5.94499 3.3082 6.1441C3.47045 6.32443 3.82768 6.52267 4.75 6.52267C5.60367 6.52267 6.08903 6.28769 6.38811 5.98319C6.70349 5.66209 6.91507 5.1591 7.00579 4.43524C7.12274 3.50212 6.96805 3.04338 6.76421 2.82557ZM9.23579 2.82557C9.03195 3.04338 8.87726 3.50212 8.99421 4.43524C9.08493 5.1591 9.29651 5.66209 9.61189 5.98319C9.91097 6.28769 10.3963 6.52267 11.25 6.52267C12.1723 6.52267 12.5295 6.32443 12.6918 6.1441C12.871 5.94499 13 5.56754 13 4.77735C13 4.01374 12.8775 3.53861 12.6303 3.22656C12.3967 2.93176 11.9369 2.64179 10.9172 2.52878C9.87298 2.41307 9.42884 2.61928 9.23579 2.82557Z"></path></svg>'},37165:b=>{b.exports='<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16"><path fill-rule="evenodd" d="M5.75 1a.75.75 0 00-.75.75v3c0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75v-3a.75.75 0 00-.75-.75h-4.5zm.75 3V2.5h3V4h-3zm-2.874-.467a.75.75 0 00-.752-1.298A1.75 1.75 0 002 3.75v9.5c0 .966.784 1.75 1.75 1.75h8.5A1.75 1.75 0 0014 13.25v-9.5a1.75 1.75 0 00-.874-1.515.75.75 0 10-.752 1.298.25.25 0 01.126.217v9.5a.25.25 0 01-.25.25h-8.5a.25.25 0 01-.25-.25v-9.5a.25.25 0 01.126-.217z"></path></svg>'},38440:b=>{b.exports='<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" viewBox="0 0 28 28" version="1.1"><g id="surface1"><path style=" stroke:none;fill-rule:evenodd;fill:#FFFFFF;fill-opacity:1;" d="M 14 0 C 6.265625 0 0 6.265625 0 14 C 0 20.195312 4.007812 25.425781 9.574219 27.285156 C 10.273438 27.402344 10.535156 26.984375 10.535156 26.617188 C 10.535156 26.285156 10.515625 25.183594 10.515625 24.011719 C 7 24.660156 6.089844 23.152344 5.808594 22.363281 C 5.652344 21.960938 4.972656 20.722656 4.375 20.386719 C 3.886719 20.125 3.183594 19.476562 4.359375 19.460938 C 5.460938 19.441406 6.246094 20.476562 6.511719 20.894531 C 7.769531 23.011719 9.785156 22.417969 10.585938 22.050781 C 10.710938 21.140625 11.078125 20.527344 11.480469 20.175781 C 8.363281 19.828125 5.109375 18.621094 5.109375 13.265625 C 5.109375 11.742188 5.652344 10.484375 6.546875 9.503906 C 6.402344 9.152344 5.914062 7.714844 6.683594 5.792969 C 6.683594 5.792969 7.859375 5.425781 10.535156 7.226562 C 11.652344 6.914062 12.847656 6.753906 14.035156 6.753906 C 15.226562 6.753906 16.414062 6.914062 17.535156 7.226562 C 20.210938 5.410156 21.386719 5.792969 21.386719 5.792969 C 22.152344 7.714844 21.664062 9.152344 21.523438 9.503906 C 22.417969 10.484375 22.960938 11.726562 22.960938 13.265625 C 22.960938 18.636719 19.6875 19.828125 16.574219 20.175781 C 17.078125 20.613281 17.515625 21.453125 17.515625 22.765625 C 17.515625 24.640625 17.5 26.144531 17.5 26.617188 C 17.5 26.984375 17.761719 27.421875 18.460938 27.285156 C 24.160156 25.359375 27.996094 20.015625 28 14 C 28 6.265625 21.734375 0 14 0 Z M 14 0 "></path></g></svg>'},46279:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M10 3h3v1h-1v9l-1 1H4l-1-1V4H2V3h3V2a1 1 0 0 1 1-1h3a1 1 0 0 1 1 1v1zM9 2H6v1h3V2zM4 13h7V4H4v9zm2-8H5v7h1V5zm1 0h1v7H7V5zm2 0h1v7H9V5z" fill="#cccccc"></path></svg>'},19443:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M8 4C8.35556 4 8.71111 4.05333 9.06667 4.16C9.74222 4.33778 10.3289 4.67556 10.8267 5.17333C11.3244 5.67111 11.6622 6.25778 11.84 6.93333C11.9467 7.28889 12 7.64444 12 8C12 8.35556 11.9467 8.71111 11.84 9.06667C11.6622 9.74222 11.3244 10.3289 10.8267 10.8267C10.3289 11.3244 9.74222 11.6622 9.06667 11.84C8.71111 11.9467 8.35556 12 8 12C7.64444 12 7.28889 11.9467 6.93333 11.84C6.25778 11.6622 5.67111 11.3244 5.17333 10.8267C4.67556 10.3289 4.33778 9.74222 4.16 9.06667C4.05333 8.71111 4 8.35556 4 8C4 7.64444 4.03556 7.30667 4.10667 6.98667C4.21333 6.63111 4.35556 6.29333 4.53333 5.97333C4.88889 5.36889 5.36889 4.88889 5.97333 4.53333C6.29333 4.35556 6.61333 4.23111 6.93333 4.16C7.28889 4.05333 7.64444 4 8 4Z" fill="#CCCCCC"></path></svg>'},83962:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M2.40706 15L1 13.5929L3.35721 9.46781L3.52339 9.25025L11.7736 1L13.2321 1L15 2.76791V4.22636L6.74975 12.4766L6.53219 12.6428L2.40706 15ZM2.40706 13.5929L6.02053 11.7474L14.2708 3.49714L12.5029 1.72923L4.25262 9.97947L2.40706 13.5929Z" fill="#C5C5C5"></path><path fill-rule="evenodd" clip-rule="evenodd" d="M5.64642 12.3536L3.64642 10.3536L4.35353 9.64645L6.35353 11.6464L5.64642 12.3536Z" fill="#C5C5C5"></path></svg>'},93492:b=>{b.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M8.6 1c1.6.1 3.1.9 4.2 2 1.3 1.4 2 3.1 2 5.1 0 1.6-.6 3.1-1.6 4.4-1 1.2-2.4 2.1-4 2.4-1.6.3-3.2.1-4.6-.7-1.4-.8-2.5-2-3.1-3.5C.9 9.2.8 7.5 1.3 6c.5-1.6 1.4-2.9 2.8-3.8C5.4 1.3 7 .9 8.6 1zm.5 12.9c1.3-.3 2.5-1 3.4-2.1.8-1.1 1.3-2.4 1.2-3.8 0-1.6-.6-3.2-1.7-4.3-1-1-2.2-1.6-3.6-1.7-1.3-.1-2.7.2-3.8 1-1.1.8-1.9 1.9-2.3 3.3-.4 1.3-.4 2.7.2 4 .6 1.3 1.5 2.3 2.7 3 1.2.7 2.6.9 3.9.6zM7.9 7.5L10.3 5l.7.7-2.4 2.5 2.4 2.5-.7.7-2.4-2.5-2.4 2.5-.7-.7 2.4-2.5-2.4-2.5.7-.7 2.4 2.5z"></path></svg>'},92359:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M9.1 4.4L8.6 2H7.4L6.9 4.4L6.2 4.7L4.2 3.4L3.3 4.2L4.6 6.2L4.4 6.9L2 7.4V8.6L4.4 9.1L4.7 9.9L3.4 11.9L4.2 12.7L6.2 11.4L7 11.7L7.4 14H8.6L9.1 11.6L9.9 11.3L11.9 12.6L12.7 11.8L11.4 9.8L11.7 9L14 8.6V7.4L11.6 6.9L11.3 6.1L12.6 4.1L11.8 3.3L9.8 4.6L9.1 4.4ZM9.4 1L9.9 3.4L12 2.1L14 4.1L12.6 6.2L15 6.6V9.4L12.6 9.9L14 12L12 14L9.9 12.6L9.4 15H6.6L6.1 12.6L4 13.9L2 11.9L3.4 9.8L1 9.4V6.6L3.4 6.1L2.1 4L4.1 2L6.2 3.4L6.6 1H9.4ZM10 8C10 9.1 9.1 10 8 10C6.9 10 6 9.1 6 8C6 6.9 6.9 6 8 6C9.1 6 10 6.9 10 8ZM8 9C8.6 9 9 8.6 9 8C9 7.4 8.6 7 8 7C7.4 7 7 7.4 7 8C7 8.6 7.4 9 8 9Z" fill="#C5C5C5"></path></svg>'},80459:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M6.00012 13H7.00012L7.00012 7.00001L13.0001 7.00001V6.00001L7.00012 6.00001L7.00012 3H6.00012L6.00012 6.00001L3.00012 6.00001V7.00001H6.00012L6.00012 13Z" fill="#C5C5C5"></path><path fill-rule="evenodd" clip-rule="evenodd" d="M2.50012 2H13.5001L14.0001 2.5V13.5L13.5001 14H2.50012L2.00012 13.5V2.5L2.50012 2ZM3.00012 13H13.0001V3H3.00012V13Z" fill="#C5C5C5"></path></svg>'},40027:b=>{b.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path d="M7.50002 1C6.21445 1 4.95774 1.38123 3.88882 2.09546C2.8199 2.80969 1.98674 3.82485 1.49478 5.01257C1.00281 6.20029 0.874098 7.50719 1.1249 8.76807C1.37571 10.0289 1.99479 11.1872 2.90383 12.0962C3.81287 13.0052 4.97108 13.6243 6.23196 13.8751C7.49283 14.1259 8.79973 13.9972 9.98745 13.5052C11.1752 13.0133 12.1903 12.1801 12.9046 11.1112C13.6188 10.0423 14 8.78558 14 7.5C14 5.77609 13.3152 4.1228 12.0962 2.90381C10.8772 1.68482 9.22393 1 7.50002 1ZM7.50002 13C6.41223 13 5.34883 12.6775 4.44436 12.0731C3.53989 11.4688 2.83501 10.6097 2.41873 9.60474C2.00244 8.59974 1.89352 7.4939 2.10574 6.427C2.31796 5.36011 2.8418 4.38015 3.61099 3.61096C4.38018 2.84177 5.36013 2.31793 6.42703 2.10571C7.49392 1.89349 8.59977 2.00242 9.60476 2.4187C10.6098 2.83498 11.4688 3.53987 12.0731 4.44434C12.6775 5.34881 13 6.4122 13 7.5C13 8.95869 12.4205 10.3576 11.3891 11.389C10.3576 12.4205 8.95871 13 7.50002 13Z"></path><circle cx="7.50002" cy="7.5" r="1"></circle></svg>'},64674:b=>{b.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path d="M6.27 10.87h.71l4.56-4.56-.71-.71-4.2 4.21-1.92-1.92L4 8.6l2.27 2.27z"></path><path fill-rule="evenodd" clip-rule="evenodd" d="M8.6 1c1.6.1 3.1.9 4.2 2 1.3 1.4 2 3.1 2 5.1 0 1.6-.6 3.1-1.6 4.4-1 1.2-2.4 2.1-4 2.4-1.6.3-3.2.1-4.6-.7-1.4-.8-2.5-2-3.1-3.5C.9 9.2.8 7.5 1.3 6c.5-1.6 1.4-2.9 2.8-3.8C5.4 1.3 7 .9 8.6 1zm.5 12.9c1.3-.3 2.5-1 3.4-2.1.8-1.1 1.3-2.4 1.2-3.8 0-1.6-.6-3.2-1.7-4.3-1-1-2.2-1.6-3.6-1.7-1.3-.1-2.7.2-3.8 1-1.1.8-1.9 1.9-2.3 3.3-.4 1.3-.4 2.7.2 4 .6 1.3 1.5 2.3 2.7 3 1.2.7 2.6.9 3.9.6z"></path></svg>'},5064:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M13.2002 2H8.01724L7.66424 2.146L1.00024 8.81V9.517L6.18324 14.7H6.89024L9.10531 12.4853C9.65832 12.7768 10.2677 12.9502 10.8945 12.9923C11.659 13.0437 12.424 12.8981 13.1162 12.5694C13.8085 12.2407 14.4048 11.74 14.8483 11.1151C15.2918 10.4902 15.5676 9.76192 15.6492 9H15.6493C15.6759 8.83446 15.6929 8.66751 15.7003 8.5C15.6989 7.30693 15.2244 6.16311 14.3808 5.31948C14.1712 5.10988 13.9431 4.92307 13.7002 4.76064V2.5L13.2002 2ZM12.7002 4.25881C12.223 4.08965 11.7162 4.00057 11.2003 4C11.0676 4 10.9405 4.05268 10.8467 4.14645C10.7529 4.24021 10.7003 4.36739 10.7003 4.5C10.7003 4.63261 10.7529 4.75979 10.8467 4.85355C10.9405 4.94732 11.0676 5 11.2003 5C11.7241 5 12.2358 5.11743 12.7002 5.33771V7.476L8.77506 11.4005C8.75767 11.4095 8.74079 11.4194 8.72449 11.4304C8.6685 11.468 8.6207 11.5166 8.58397 11.5731C8.57475 11.5874 8.56627 11.602 8.55856 11.617L6.53624 13.639L2.06124 9.163L8.22424 3H12.7002V4.25881ZM13.7002 6.0505C14.3409 6.70435 14.7003 7.58365 14.7003 8.5C14.6955 8.66769 14.6784 8.8348 14.6493 9H14.6492C14.5675 9.58097 14.3406 10.1319 13.9894 10.6019C13.6383 11.0719 13.1743 11.4457 12.6403 11.6888C12.1063 11.9319 11.5197 12.0363 10.9346 11.9925C10.5622 11.9646 10.1982 11.8772 9.85588 11.7348L13.5542 8.037L13.7002 7.683V6.0505Z" fill="#C5C5C5"></path></svg>'},95570:b=>{b.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M13.917 7A6.002 6.002 0 0 0 2.083 7H1.071a7.002 7.002 0 0 1 13.858 0h-1.012z"></path></svg>'},90346:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M4.99008 1C4.5965 1 4.21175 1.11671 3.8845 1.33538C3.55724 1.55404 3.30218 1.86484 3.15156 2.22846C3.00094 2.59208 2.96153 2.99221 3.03832 3.37823C3.1151 3.76425 3.30463 4.11884 3.58294 4.39714C3.83589 4.65009 4.15185 4.8297 4.49715 4.91798L4.49099 10.8286C4.40192 10.8517 4.31421 10.881 4.22852 10.9165C3.8649 11.0671 3.5541 11.3222 3.33544 11.6494C3.11677 11.9767 3.00006 12.3614 3.00006 12.755C3.00006 13.2828 3.20972 13.7889 3.58292 14.1621C3.95612 14.5353 4.46228 14.745 4.99006 14.745C5.38365 14.745 5.76839 14.6283 6.09565 14.4096C6.4229 14.191 6.67796 13.8802 6.82858 13.5165C6.9792 13.1529 7.01861 12.7528 6.94182 12.3668C6.86504 11.9807 6.67551 11.6262 6.3972 11.3479C6.14426 11.0949 5.8283 10.9153 5.48299 10.827V9.745H5.48915V8.80133C6.50043 10.3332 8.19531 11.374 10.1393 11.4893C10.2388 11.7413 10.3893 11.9714 10.5825 12.1648C10.8608 12.4432 11.2154 12.6328 11.6014 12.7097C11.9875 12.7866 12.3877 12.7472 12.7513 12.5966C13.115 12.446 13.4259 12.191 13.6446 11.8637C13.8633 11.5364 13.98 11.1516 13.98 10.758C13.98 10.2304 13.7705 9.72439 13.3975 9.35122C13.0245 8.97805 12.5186 8.76827 11.991 8.76801C11.5974 8.76781 11.2126 8.88435 10.8852 9.10289C10.5578 9.32144 10.3026 9.63216 10.1518 9.99577C10.0875 10.1509 10.0434 10.3127 10.0199 10.4772C7.48375 10.2356 5.48915 8.09947 5.48915 5.5C5.48915 5.33125 5.47282 5.16445 5.48915 5V4.9164C5.57823 4.89333 5.66594 4.86401 5.75162 4.82852C6.11525 4.6779 6.42604 4.42284 6.64471 4.09558C6.86337 3.76833 6.98008 3.38358 6.98008 2.99C6.98008 2.46222 6.77042 1.95605 6.39722 1.58286C6.02403 1.20966 5.51786 1 4.99008 1ZM4.99008 2C5.18593 1.9998 5.37743 2.0577 5.54037 2.16636C5.70331 2.27502 5.83035 2.42957 5.90544 2.61045C5.98052 2.79133 6.00027 2.99042 5.96218 3.18253C5.9241 3.37463 5.82989 3.55113 5.69147 3.68968C5.55306 3.82824 5.37666 3.92262 5.18459 3.9609C4.99252 3.99918 4.79341 3.97964 4.61246 3.90474C4.4315 3.82983 4.27682 3.70294 4.168 3.54012C4.05917 3.37729 4.00108 3.18585 4.00108 2.99C4.00135 2.72769 4.1056 2.47618 4.29098 2.29061C4.47637 2.10503 4.72777 2.00053 4.99008 2ZM4.99006 13.745C4.79422 13.7452 4.60271 13.6873 4.43977 13.5786C4.27684 13.47 4.14979 13.3154 4.07471 13.1345C3.99962 12.9537 3.97988 12.7546 4.01796 12.5625C4.05605 12.3704 4.15026 12.1939 4.28867 12.0553C4.42709 11.9168 4.60349 11.8224 4.79555 11.7841C4.98762 11.7458 5.18673 11.7654 5.36769 11.8403C5.54864 11.9152 5.70332 12.0421 5.81215 12.2049C5.92097 12.3677 5.97906 12.5591 5.97906 12.755C5.9788 13.0173 5.87455 13.2688 5.68916 13.4544C5.50377 13.64 5.25237 13.7445 4.99006 13.745ZM11.991 9.76801C12.1868 9.76801 12.3782 9.82607 12.541 9.93485C12.7038 10.0436 12.8307 10.1983 12.9057 10.3791C12.9806 10.56 13.0002 10.7591 12.962 10.9511C12.9238 11.1432 12.8295 11.3196 12.6911 11.458C12.5526 11.5965 12.3762 11.6908 12.1842 11.729C11.9921 11.7672 11.7931 11.7476 11.6122 11.6726C11.4313 11.5977 11.2767 11.4708 11.1679 11.308C11.0591 11.1452 11.001 10.9538 11.001 10.758C11.0013 10.4955 11.1057 10.2439 11.2913 10.0583C11.4769 9.87266 11.7285 9.76827 11.991 9.76801Z" fill="#C5C5C5"></path></svg>'},44370:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M10.5002 4.64639L8.35388 2.5H7.64677L5.50034 4.64639L6.20744 5.35349L7.3003 4.26066V5.27972H7.28082V5.73617L7.30568 5.73717C7.30768 5.84794 7.30968 5.95412 7.31169 6.05572C7.31538 6.24322 7.33201 6.43462 7.36158 6.62994C7.39114 6.82525 7.42994 7.02056 7.47799 7.21587C7.52603 7.41119 7.59255 7.62017 7.67755 7.84283C7.83276 8.22173 8.02124 8.56548 8.24297 8.87408C8.4647 9.18267 8.70307 9.47173 8.95806 9.74127C9.21306 10.0108 9.46621 10.2764 9.71751 10.5381C9.9688 10.7999 10.1961 11.0792 10.3993 11.376C10.6026 11.6729 10.767 11.9971 10.8927 12.3487C11.0183 12.7002 11.0812 13.1045 11.0812 13.5616V14.4463H12.5003V13.5616C12.4929 13.042 12.4375 12.5792 12.334 12.1729C12.2305 11.7667 12.0882 11.3995 11.9071 11.0713C11.7261 10.7432 11.5246 10.4444 11.3029 10.1749C11.0812 9.90533 10.8502 9.64752 10.61 9.40142C10.3698 9.15533 10.1388 8.90923 9.91707 8.66314C9.69533 8.41705 9.49392 8.15533 9.31284 7.87798C9.13176 7.60064 8.98763 7.29595 8.88046 6.96392C8.77329 6.63189 8.7197 6.25494 8.7197 5.83306V5.27972H8.71901V4.27935L9.79314 5.3535L10.5002 4.64639ZM7.04245 9.74127C7.15517 9.62213 7.26463 9.49917 7.37085 9.3724C7.12665 9.01878 6.92109 8.63423 6.75218 8.22189L6.74317 8.19952C6.70951 8.11134 6.67794 8.02386 6.6486 7.93713C6.47774 8.19261 6.28936 8.43461 6.08345 8.66314C5.86172 8.90923 5.63074 9.15533 5.39053 9.40142C5.15032 9.64752 4.91935 9.90533 4.69761 10.1749C4.47588 10.4444 4.27447 10.7432 4.09338 11.0713C3.9123 11.3995 3.77002 11.7667 3.66654 12.1729C3.56307 12.5792 3.50764 13.042 3.50024 13.5616V14.4463H4.91935V13.5616C4.91935 13.1045 4.98217 12.7002 5.10782 12.3487C5.23347 11.9971 5.39792 11.6729 5.60118 11.376C5.80444 11.0792 6.03171 10.7999 6.28301 10.5381C6.53431 10.2764 6.78746 10.0108 7.04245 9.74127Z" fill="#424242"></path></svg>'},20628:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M7.99976 1H6.99976V3H1.49976L0.999756 3.5V7.5L1.49976 8H6.99976V15H7.99976V8H12.4898L12.8298 7.87L15.0098 5.87V5.13L12.8298 3.13L12.4998 3H7.99976V1ZM12.2898 7H1.99976V4H12.2898L13.9198 5.5L12.2898 7ZM4.99976 5H9.99976V6H4.99976V5Z" fill="#C5C5C5"></path></svg>'},15010:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M14 7V8H8V14H7V8H1V7H7V1H8V7H14Z" fill="#C5C5C5"></path></svg>'},14268:b=>{b.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M5.616 4.928a2.487 2.487 0 0 1-1.119.922c-.148.06-.458.138-.458.138v5.008a2.51 2.51 0 0 1 1.579 1.062c.273.412.419.895.419 1.388.008.343-.057.684-.19 1A2.485 2.485 0 0 1 3.5 15.984a2.482 2.482 0 0 1-1.388-.419A2.487 2.487 0 0 1 1.05 13c.095-.486.331-.932.68-1.283.349-.343.79-.579 1.269-.68V5.949a2.6 2.6 0 0 1-1.269-.68 2.503 2.503 0 0 1-.68-1.283 2.487 2.487 0 0 1 1.06-2.565A2.49 2.49 0 0 1 3.5 1a2.504 2.504 0 0 1 1.807.729 2.493 2.493 0 0 1 .729 1.81c.002.494-.144.978-.42 1.389zm-.756 7.861a1.5 1.5 0 0 0-.552-.579 1.45 1.45 0 0 0-.77-.21 1.495 1.495 0 0 0-1.47 1.79 1.493 1.493 0 0 0 1.18 1.179c.288.058.586.03.86-.08.276-.117.512-.312.68-.56.15-.226.235-.49.249-.76a1.51 1.51 0 0 0-.177-.78zM2.708 4.741c.247.161.536.25.83.25.271 0 .538-.075.77-.211a1.514 1.514 0 0 0 .729-1.359 1.513 1.513 0 0 0-.25-.76 1.551 1.551 0 0 0-.68-.56 1.49 1.49 0 0 0-.86-.08 1.494 1.494 0 0 0-1.179 1.18c-.058.288-.03.586.08.86.117.276.312.512.56.68zm10.329 6.296c.48.097.922.335 1.269.68.466.47.729 1.107.725 1.766.002.493-.144.977-.42 1.388a2.499 2.499 0 0 1-4.532-.899 2.5 2.5 0 0 1 1.067-2.565c.267-.183.571-.308.889-.37V5.489a1.5 1.5 0 0 0-1.5-1.499H8.687l1.269 1.27-.71.709L7.117 3.84v-.7l2.13-2.13.71.711-1.269 1.27h1.85a2.484 2.484 0 0 1 2.312 1.541c.125.302.189.628.187.957v5.548zm.557 3.509a1.493 1.493 0 0 0 .191-1.89 1.552 1.552 0 0 0-.68-.559 1.49 1.49 0 0 0-.86-.08 1.493 1.493 0 0 0-1.179 1.18 1.49 1.49 0 0 0 .08.86 1.496 1.496 0 0 0 2.448.49z"></path></svg>'},30340:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M7.38893 12.9906L6.11891 11.7205L6.78893 11.0206L8.91893 13.1506V13.8505L6.78893 15.9805L6.07893 15.2706L7.34892 14.0006H5.49892C5.17024 14.0019 4.84458 13.9381 4.54067 13.8129C4.23675 13.6878 3.96061 13.5037 3.7282 13.2713C3.49579 13.0389 3.31171 12.7627 3.18654 12.4588C3.06137 12.1549 2.99759 11.8292 2.99892 11.5006V5.95052C2.5198 5.84851 2.07944 5.61279 1.72893 5.27059C1.3808 4.91884 1.14393 4.47238 1.0479 3.98689C0.951867 3.50141 1.00092 2.9984 1.18892 2.54061C1.37867 2.08436 1.69938 1.69458 2.11052 1.42049C2.52166 1.14639 3.00479 1.00024 3.49892 1.00057C3.84188 0.993194 4.18256 1.05787 4.49892 1.19051C4.80197 1.31518 5.07732 1.49871 5.30904 1.73042C5.54075 1.96214 5.72425 2.23755 5.84892 2.54061C5.98157 2.85696 6.0463 3.19765 6.03893 3.54061C6.03926 4.03474 5.89316 4.51789 5.61906 4.92903C5.34497 5.34017 4.95516 5.6608 4.49892 5.85054C4.35057 5.91224 4.19649 5.95915 4.03893 5.99056V11.4906C4.03893 11.8884 4.19695 12.2699 4.47826 12.5512C4.75956 12.8325 5.1411 12.9906 5.53893 12.9906H7.38893ZM2.70894 4.74056C2.95497 4.90376 3.24368 4.99072 3.53893 4.99056C3.81026 4.99066 4.07654 4.91718 4.3094 4.77791C4.54227 4.63864 4.73301 4.43877 4.86128 4.19966C4.98956 3.96056 5.05057 3.69116 5.03783 3.42012C5.02508 3.14908 4.93907 2.88661 4.78893 2.6606C4.62119 2.4121 4.38499 2.21751 4.10893 2.10054C3.83645 1.98955 3.53719 1.96176 3.24892 2.02059C2.95693 2.07705 2.68852 2.2196 2.47823 2.42989C2.26793 2.64018 2.12539 2.90853 2.06892 3.20052C2.0101 3.4888 2.03792 3.78802 2.14891 4.0605C2.26588 4.33656 2.46043 4.57282 2.70894 4.74056ZM13.0389 11.0406C13.5196 11.1384 13.9612 11.3747 14.309 11.7206C14.7766 12.191 15.039 12.8273 15.0389 13.4906C15.0393 13.9847 14.8932 14.4679 14.6191 14.879C14.345 15.2902 13.9552 15.6109 13.499 15.8007C13.0416 15.9915 12.5378 16.0421 12.0516 15.946C11.5654 15.85 11.1187 15.6117 10.7683 15.2612C10.4179 14.9108 10.1795 14.4641 10.0835 13.9779C9.98746 13.4917 10.0381 12.988 10.2289 12.5306C10.4218 12.0768 10.7412 11.688 11.1489 11.4106C11.4177 11.2286 11.7204 11.1028 12.0389 11.0406V5.4906C12.0389 5.09278 11.8809 4.71124 11.5996 4.42993C11.3183 4.14863 10.9368 3.9906 10.5389 3.9906H8.68896L9.95892 5.26062L9.24896 5.97058L7.11893 3.84058V3.14063L9.24896 1.01062L9.95892 1.72058L8.68896 2.9906H10.5389C10.8676 2.98928 11.1933 3.05305 11.4972 3.17822C11.8011 3.30339 12.0772 3.48744 12.3096 3.71985C12.542 3.95226 12.7262 4.22844 12.8513 4.53235C12.9765 4.83626 13.0403 5.16193 13.0389 5.4906V11.0406ZM12.6879 14.9829C13.0324 14.9483 13.3542 14.7956 13.5989 14.5507C13.8439 14.306 13.9966 13.984 14.0313 13.6395C14.0659 13.295 13.9803 12.9492 13.7889 12.6606C13.6212 12.4121 13.385 12.2176 13.1089 12.1006C12.8365 11.9896 12.5372 11.9618 12.249 12.0206C11.957 12.0771 11.6886 12.2196 11.4783 12.4299C11.268 12.6402 11.1254 12.9086 11.069 13.2006C11.0101 13.4888 11.0379 13.7881 11.1489 14.0605C11.2659 14.3366 11.4604 14.5729 11.7089 14.7406C11.9975 14.9319 12.3434 15.0175 12.6879 14.9829Z" fill="#C5C5C5"></path></svg>'},90659:b=>{b.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M5.61594 4.92769C5.34304 5.33899 4.95319 5.66062 4.49705 5.8497C4.34891 5.91013 4.03897 5.9881 4.03897 5.9881V10.9958C4.19686 11.027 4.35086 11.0738 4.499 11.1362C4.95513 11.3272 5.34304 11.6469 5.61789 12.0582C5.89079 12.4695 6.03699 12.9529 6.03699 13.4461C6.04478 13.7891 5.98046 14.1303 5.84791 14.446C5.72315 14.7482 5.53992 15.023 5.30796 15.255C5.07794 15.487 4.80114 15.6702 4.499 15.7949C4.18322 15.9275 3.84209 15.9918 3.49902 15.984C3.00585 15.986 2.52243 15.8398 2.11113 15.5649C1.69983 15.292 1.3782 14.9022 1.18912 14.446C1.00198 13.988 0.953253 13.485 1.04877 12.9997C1.14428 12.5143 1.38015 12.0679 1.72907 11.717C2.07799 11.374 2.51853 11.1381 2.99805 11.0367V5.94911C2.52048 5.8458 2.07994 5.61189 1.72907 5.26881C1.38015 4.91794 1.14428 4.47155 1.04877 3.98618C0.951304 3.50081 1.00004 2.99789 1.18912 2.53981C1.3782 2.08368 1.69983 1.69382 2.11113 1.42092C2.52048 1.14607 3.0039 0.999877 3.49902 0.999877C3.84014 0.99403 4.18127 1.05836 4.49705 1.18896C4.79919 1.31371 5.07404 1.49695 5.30601 1.72891C5.53797 1.96087 5.7212 2.23767 5.84596 2.53981C5.97851 2.8556 6.04284 3.19672 6.03504 3.5398C6.03699 4.03296 5.89079 4.51639 5.61594 4.92769ZM4.85962 12.7892C4.73097 12.5494 4.53994 12.3486 4.30797 12.2102C4.07601 12.0699 3.80896 11.9958 3.538 11.9997C3.24171 11.9997 2.95322 12.0855 2.70761 12.2492C2.46005 12.4168 2.26512 12.6527 2.14816 12.9295C2.03706 13.2024 2.00977 13.5006 2.06824 13.7891C2.12477 14.0796 2.26707 14.3486 2.47759 14.5591C2.68812 14.7696 2.95517 14.9119 3.24756 14.9685C3.53606 15.0269 3.8343 14.9996 4.1072 14.8885C4.38399 14.7716 4.61986 14.5766 4.7875 14.3291C4.93759 14.103 5.02336 13.8398 5.037 13.5689C5.0487 13.2979 4.98827 13.0289 4.85962 12.7892ZM2.70761 4.74056C2.95517 4.90235 3.24366 4.99006 3.538 4.99006C3.80896 4.99006 4.07601 4.91599 4.30797 4.77954C4.53994 4.63919 4.73097 4.44037 4.85962 4.2006C4.98827 3.96084 5.05065 3.69184 5.037 3.42089C5.02336 3.14994 4.93759 2.88679 4.7875 2.66067C4.61986 2.41311 4.38399 2.21818 4.1072 2.10122C3.8343 1.99011 3.53606 1.96282 3.24756 2.0213C2.95712 2.07783 2.68812 2.22013 2.47759 2.43065C2.26707 2.64118 2.12477 2.90823 2.06824 3.20062C2.00977 3.48911 2.03706 3.78735 2.14816 4.06025C2.26512 4.33705 2.46005 4.57292 2.70761 4.74056ZM13.0368 11.0368C13.5164 11.1342 13.9588 11.372 14.3058 11.7171C14.7717 12.1868 15.0348 12.8243 15.0309 13.4831C15.0329 13.9763 14.8867 14.4597 14.6119 14.871C14.339 15.2823 13.9491 15.6039 13.493 15.793C13.0368 15.984 12.532 16.0347 12.0466 15.9392C11.5612 15.8437 11.1148 15.6059 10.764 15.255C10.415 14.9041 10.1753 14.4578 10.0798 13.9724C9.98425 13.487 10.0349 12.9841 10.226 12.526C10.4189 12.0738 10.7386 11.6839 11.146 11.4071C11.4131 11.2239 11.7172 11.0991 12.0349 11.0368V7.4891H13.0368V11.0368ZM13.5943 14.5455C13.8399 14.3018 13.992 13.9802 14.0271 13.6352C14.0622 13.2921 13.9764 12.9451 13.7854 12.6566C13.6177 12.4091 13.3819 12.2141 13.1051 12.0972C12.8322 11.9861 12.5339 11.9588 12.2454 12.0173C11.955 12.0738 11.686 12.2161 11.4755 12.4266C11.2649 12.6371 11.1226 12.9042 11.0661 13.1966C11.0076 13.4851 11.0349 13.7833 11.146 14.0562C11.263 14.333 11.4579 14.5689 11.7055 14.7365C11.994 14.9275 12.339 15.0133 12.684 14.9782C13.0271 14.9431 13.3507 14.7911 13.5943 14.5455Z"></path><path fill-rule="evenodd" clip-rule="evenodd" d="M11.6876 3.40036L10 5.088L10.7071 5.7951L12.3947 4.10747L14.0824 5.7951L14.7895 5.088L13.1019 3.40036L14.7895 1.71272L14.0824 1.00562L12.3947 2.69325L10.7071 1.00562L10 1.71272L11.6876 3.40036Z"></path></svg>'},83344:b=>{b.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path d="M4.49705 5.8497C4.95319 5.66062 5.34304 5.33899 5.61594 4.92769C5.89079 4.51639 6.03699 4.03296 6.03504 3.5398C6.04284 3.19672 5.97851 2.8556 5.84596 2.53981C5.7212 2.23767 5.53797 1.96087 5.30601 1.72891C5.07404 1.49695 4.79919 1.31371 4.49705 1.18896C4.18127 1.05836 3.84014 0.99403 3.49902 0.999877C3.0039 0.999877 2.52048 1.14607 2.11113 1.42092C1.69983 1.69382 1.3782 2.08368 1.18912 2.53981C1.00004 2.99789 0.951304 3.50081 1.04877 3.98618C1.14428 4.47155 1.38015 4.91794 1.72907 5.26881C2.07994 5.61189 2.52048 5.8458 2.99805 5.94911V11.0367C2.51853 11.1381 2.07799 11.374 1.72907 11.717C1.38015 12.0679 1.14428 12.5143 1.04877 12.9997C0.953253 13.485 1.00198 13.988 1.18912 14.446C1.3782 14.9022 1.69983 15.292 2.11113 15.5649C2.52243 15.8398 3.00585 15.986 3.49902 15.984C3.84209 15.9918 4.18322 15.9275 4.499 15.7949C4.80114 15.6702 5.07794 15.487 5.30796 15.255C5.53992 15.023 5.72315 14.7482 5.84791 14.446C5.98046 14.1303 6.04478 13.7891 6.03699 13.4461C6.03699 12.9529 5.89079 12.4695 5.61789 12.0582C5.34304 11.6469 4.95513 11.3272 4.499 11.1362C4.35086 11.0738 4.19686 11.027 4.03897 10.9958V5.9881C4.03897 5.9881 4.34891 5.91013 4.49705 5.8497ZM4.30797 12.2102C4.53994 12.3486 4.73097 12.5494 4.85962 12.7892C4.98827 13.0289 5.0487 13.2979 5.037 13.5689C5.02336 13.8398 4.93759 14.103 4.7875 14.3291C4.61986 14.5766 4.38399 14.7716 4.1072 14.8885C3.8343 14.9996 3.53606 15.0269 3.24756 14.9685C2.95517 14.9119 2.68812 14.7696 2.47759 14.5591C2.26707 14.3486 2.12477 14.0796 2.06824 13.7891C2.00977 13.5006 2.03706 13.2024 2.14816 12.9295C2.26512 12.6527 2.46005 12.4168 2.70761 12.2492C2.95322 12.0855 3.24171 11.9997 3.538 11.9997C3.80896 11.9958 4.07601 12.0699 4.30797 12.2102ZM3.538 4.99006C3.24366 4.99006 2.95517 4.90235 2.70761 4.74056C2.46005 4.57292 2.26512 4.33705 2.14816 4.06025C2.03706 3.78735 2.00977 3.48911 2.06824 3.20062C2.12477 2.90823 2.26707 2.64118 2.47759 2.43065C2.68812 2.22013 2.95712 2.07783 3.24756 2.0213C3.53606 1.96282 3.8343 1.99011 4.1072 2.10122C4.38399 2.21818 4.61986 2.41311 4.7875 2.66067C4.93759 2.88679 5.02336 3.14994 5.037 3.42089C5.05065 3.69184 4.98827 3.96084 4.85962 4.2006C4.73097 4.44037 4.53994 4.63919 4.30797 4.77954C4.07601 4.91599 3.80896 4.99006 3.538 4.99006Z"></path><path fill-rule="evenodd" clip-rule="evenodd" d="M15.0543 13.5C15.0543 14.8807 13.935 16 12.5543 16C11.1736 16 10.0543 14.8807 10.0543 13.5C10.0543 12.1193 11.1736 11 12.5543 11C13.935 11 15.0543 12.1193 15.0543 13.5ZM12.5543 15C13.3827 15 14.0543 14.3284 14.0543 13.5C14.0543 12.6716 13.3827 12 12.5543 12C11.7258 12 11.0543 12.6716 11.0543 13.5C11.0543 14.3284 11.7258 15 12.5543 15Z"></path><circle cx="12.5543" cy="7.75073" r="1"></circle><circle cx="12.5543" cy="3.50146" r="1"></circle></svg>'},9649:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" clip-rule="evenodd" d="M2.14648 6.3065L6.16649 2.2865L6.87359 2.2865L10.8936 6.3065L10.1865 7.0136L6.97998 3.8071L6.97998 5.69005C6.97998 8.50321 7.58488 10.295 8.70856 11.3953C9.83407 12.4974 11.5857 13.0101 14.13 13.0101L14.48 13.0101L14.48 14.0101L14.13 14.0101C11.4843 14.0101 9.4109 13.4827 8.00891 12.1098C6.60509 10.7351 5.97998 8.61689 5.97998 5.69005L5.97998 3.88722L2.85359 7.01361L2.14648 6.3065Z" fill="#C5C5C5"></path></svg>'},72362:b=>{b.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M7.16 3.5C4.73 5.06 3.55 6.67 3.55 9.36c.16-.05.3-.05.44-.05 1.27 0 2.5.86 2.5 2.41 0 1.61-1.03 2.61-2.5 2.61-1.9 0-2.99-1.52-2.99-4.25 0-3.8 1.75-6.53 5.02-8.42L7.16 3.5zm7 0c-2.43 1.56-3.61 3.17-3.61 5.86.16-.05.3-.05.44-.05 1.27 0 2.5.86 2.5 2.41 0 1.61-1.03 2.61-2.5 2.61-1.89 0-2.98-1.52-2.98-4.25 0-3.8 1.75-6.53 5.02-8.42l1.14 1.84h-.01z"></path></svg>'},98923:b=>{b.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M10.7099 1.29L13.7099 4.29L13.9999 5V14L12.9999 15H3.99994L2.99994 14V2L3.99994 1H9.99994L10.7099 1.29ZM3.99994 14H12.9999V5L9.99994 2H3.99994V14ZM8 6H6V7H8V9H9V7H11V6H9V4H8V6ZM6 11H11V12H6V11Z"></path></svg>'},96855:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M7.54883 10.0781C8.00911 10.2604 8.42839 10.502 8.80664 10.8027C9.1849 11.1035 9.50846 11.4521 9.77734 11.8486C10.0462 12.2451 10.2536 12.6712 10.3994 13.127C10.5452 13.5827 10.6204 14.0612 10.625 14.5625V15H9.75V14.5625C9.75 14.0202 9.64746 13.5098 9.44238 13.0312C9.2373 12.5527 8.95475 12.1357 8.59473 11.7803C8.2347 11.4248 7.81771 11.1445 7.34375 10.9395C6.86979 10.7344 6.35938 10.6296 5.8125 10.625C5.27018 10.625 4.75977 10.7275 4.28125 10.9326C3.80273 11.1377 3.38574 11.4202 3.03027 11.7803C2.6748 12.1403 2.39453 12.5573 2.18945 13.0312C1.98438 13.5052 1.87956 14.0156 1.875 14.5625V15H1V14.5625C1 14.0658 1.07292 13.5872 1.21875 13.127C1.36458 12.6667 1.57422 12.2406 1.84766 11.8486C2.12109 11.4567 2.44466 11.1104 2.81836 10.8096C3.19206 10.5088 3.61133 10.265 4.07617 10.0781C3.87109 9.93685 3.68652 9.77279 3.52246 9.58594C3.3584 9.39909 3.2194 9.19857 3.10547 8.98438C2.99154 8.77018 2.90495 8.54232 2.8457 8.30078C2.78646 8.05924 2.75456 7.81315 2.75 7.5625C2.75 7.13867 2.82975 6.74219 2.98926 6.37305C3.14876 6.00391 3.36751 5.68034 3.64551 5.40234C3.9235 5.12435 4.24707 4.9056 4.61621 4.74609C4.98535 4.58659 5.38411 4.50456 5.8125 4.5C6.23633 4.5 6.63281 4.57975 7.00195 4.73926C7.37109 4.89876 7.69466 5.11751 7.97266 5.39551C8.25065 5.6735 8.4694 5.99707 8.62891 6.36621C8.78841 6.73535 8.87044 7.13411 8.875 7.5625C8.875 7.81315 8.84538 8.05697 8.78613 8.29395C8.72689 8.53092 8.63802 8.75879 8.51953 8.97754C8.40104 9.19629 8.26204 9.39909 8.10254 9.58594C7.94303 9.77279 7.75846 9.93685 7.54883 10.0781ZM5.8125 9.75C6.11328 9.75 6.39583 9.69303 6.66016 9.5791C6.92448 9.46517 7.15462 9.31022 7.35059 9.11426C7.54655 8.91829 7.70378 8.68587 7.82227 8.41699C7.94076 8.14811 8 7.86328 8 7.5625C8 7.26172 7.94303 6.97917 7.8291 6.71484C7.71517 6.45052 7.55794 6.22038 7.35742 6.02441C7.1569 5.82845 6.92448 5.67122 6.66016 5.55273C6.39583 5.43424 6.11328 5.375 5.8125 5.375C5.51172 5.375 5.22917 5.43197 4.96484 5.5459C4.70052 5.65983 4.4681 5.81706 4.26758 6.01758C4.06706 6.2181 3.90983 6.45052 3.7959 6.71484C3.68197 6.97917 3.625 7.26172 3.625 7.5625C3.625 7.86328 3.68197 8.14583 3.7959 8.41016C3.90983 8.67448 4.06478 8.9069 4.26074 9.10742C4.45671 9.30794 4.68913 9.46517 4.95801 9.5791C5.22689 9.69303 5.51172 9.75 5.8125 9.75ZM15 1V8H13.25L10.625 10.625V8H9.75V7.125H11.5V8.5127L12.8877 7.125H14.125V1.875H5.375V3.44727C5.22917 3.46549 5.08333 3.48828 4.9375 3.51562C4.79167 3.54297 4.64583 3.58398 4.5 3.63867V1H15Z" fill="#C5C5C5"></path></svg>'},15493:b=>{b.exports='<svg viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg"><path d="M9.12 4.37333L8.58667 1.97333H7.41333L6.88 4.37333L6.18667 4.69333L4.21333 3.41333L3.30667 4.21333L4.58667 6.18667L4.42667 6.88L2.02667 7.41333V8.58667L4.42667 9.12L4.69333 9.92L3.41333 11.8933L4.21333 12.6933L6.18667 11.4133L6.98667 11.68L7.41333 13.9733H8.58667L9.12 11.5733L9.92 11.3067L11.8933 12.5867L12.6933 11.7867L11.4133 9.81333L11.68 9.01333L14.0267 8.58667V7.41333L11.6267 6.88L11.3067 6.08L12.5867 4.10667L11.7867 3.30667L9.81333 4.58667L9.12 4.37333ZM9.38667 1.01333L9.92 3.41333L12 2.08L14.0267 4.10667L12.5867 6.18667L14.9867 6.61333V9.38667L12.5867 9.92L14.0267 12L12 13.9733L9.92 12.5867L9.38667 14.9867H6.61333L6.08 12.5867L4 13.92L2.02667 11.8933L3.41333 9.81333L1.01333 9.38667V6.61333L3.41333 6.08L2.08 4L4.10667 1.97333L6.18667 3.41333L6.61333 1.01333H9.38667ZM10.0267 8C10.0267 8.53333 9.81333 8.99556 9.38667 9.38667C8.99556 9.77778 8.53333 9.97333 8 9.97333C7.46667 9.97333 7.00444 9.77778 6.61333 9.38667C6.22222 8.99556 6.02667 8.53333 6.02667 8C6.02667 7.46667 6.22222 7.00444 6.61333 6.61333C7.00444 6.18667 7.46667 5.97333 8 5.97333C8.53333 5.97333 8.99556 6.18667 9.38667 6.61333C9.81333 7.00444 10.0267 7.46667 10.0267 8ZM8 9.01333C8.28444 9.01333 8.51556 8.92444 8.69333 8.74667C8.90667 8.53333 9.01333 8.28444 9.01333 8C9.01333 7.71556 8.90667 7.48444 8.69333 7.30667C8.51556 7.09333 8.28444 6.98667 8 6.98667C7.71556 6.98667 7.46667 7.09333 7.25333 7.30667C7.07556 7.48444 6.98667 7.71556 6.98667 8C6.98667 8.28444 7.07556 8.53333 7.25333 8.74667C7.46667 8.92444 7.71556 9.01333 8 9.01333Z" fill="#CCCCCC"></path></svg>'},61779:b=>{b.exports='<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M17.28 7.78a.75.75 0 00-1.06-1.06l-9.5 9.5a.75.75 0 101.06 1.06l9.5-9.5z"></path><path fill-rule="evenodd" d="M12 1C5.925 1 1 5.925 1 12s4.925 11 11 11 11-4.925 11-11S18.075 1 12 1zM2.5 12a9.5 9.5 0 1119 0 9.5 9.5 0 01-19 0z"></path></svg>'},70596:b=>{b.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path d="M5.39804 10.8069C5.57428 10.9312 5.78476 10.9977 6.00043 10.9973C6.21633 10.9975 6.42686 10.93 6.60243 10.8043C6.77993 10.6739 6.91464 10.4936 6.98943 10.2863L7.43643 8.91335C7.55086 8.56906 7.74391 8.25615 8.00028 7.99943C8.25665 7.74272 8.56929 7.54924 8.91343 7.43435L10.3044 6.98335C10.4564 6.92899 10.5936 6.84019 10.7055 6.7239C10.8174 6.60762 10.9008 6.467 10.9492 6.31308C10.9977 6.15916 11.0098 5.99611 10.9847 5.83672C10.9596 5.67732 10.8979 5.52591 10.8044 5.39435C10.6703 5.20842 10.4794 5.07118 10.2604 5.00335L8.88543 4.55635C8.54091 4.44212 8.22777 4.24915 7.97087 3.99277C7.71396 3.73638 7.52035 3.42363 7.40543 3.07935L6.95343 1.69135C6.88113 1.48904 6.74761 1.31428 6.57143 1.19135C6.43877 1.09762 6.28607 1.03614 6.12548 1.01179C5.96489 0.987448 5.80083 1.00091 5.64636 1.05111C5.49188 1.1013 5.35125 1.18685 5.23564 1.30095C5.12004 1.41505 5.03265 1.55454 4.98043 1.70835L4.52343 3.10835C4.40884 3.44317 4.21967 3.74758 3.97022 3.9986C3.72076 4.24962 3.41753 4.44067 3.08343 4.55735L1.69243 5.00535C1.54065 5.05974 1.40352 5.14852 1.29177 5.26474C1.18001 5.38095 1.09666 5.52145 1.04824 5.67523C0.999819 5.82902 0.987639 5.99192 1.01265 6.1512C1.03767 6.31048 1.0992 6.46181 1.19243 6.59335C1.32027 6.7728 1.50105 6.90777 1.70943 6.97935L3.08343 7.42435C3.52354 7.57083 3.90999 7.84518 4.19343 8.21235C4.35585 8.42298 4.4813 8.65968 4.56443 8.91235L5.01643 10.3033C5.08846 10.5066 5.22179 10.6826 5.39804 10.8069ZM5.48343 3.39235L6.01043 2.01535L6.44943 3.39235C6.61312 3.8855 6.88991 4.33351 7.25767 4.70058C7.62544 5.06765 8.07397 5.34359 8.56743 5.50635L9.97343 6.03535L8.59143 6.48335C8.09866 6.64764 7.65095 6.92451 7.28382 7.29198C6.9167 7.65945 6.64026 8.10742 6.47643 8.60035L5.95343 9.97835L5.50443 8.59935C5.34335 8.10608 5.06943 7.65718 4.70443 7.28835C4.3356 6.92031 3.88653 6.64272 3.39243 6.47735L2.01443 5.95535L3.40043 5.50535C3.88672 5.33672 4.32775 5.05855 4.68943 4.69235C5.04901 4.32464 5.32049 3.88016 5.48343 3.39235ZM11.5353 14.8494C11.6713 14.9456 11.8337 14.9973 12.0003 14.9974C12.1654 14.9974 12.3264 14.9464 12.4613 14.8514C12.6008 14.7529 12.7058 14.6129 12.7613 14.4514L13.0093 13.6894C13.0625 13.5309 13.1515 13.3869 13.2693 13.2684C13.3867 13.1498 13.5307 13.0611 13.6893 13.0094L14.4613 12.7574C14.619 12.7029 14.7557 12.6004 14.8523 12.4644C14.9257 12.3614 14.9736 12.2424 14.9921 12.1173C15.0106 11.9922 14.9992 11.8645 14.9588 11.7447C14.9184 11.6249 14.8501 11.5163 14.7597 11.428C14.6692 11.3396 14.5591 11.2739 14.4383 11.2364L13.6743 10.9874C13.5162 10.9348 13.3724 10.8462 13.2544 10.7285C13.1364 10.6109 13.0473 10.4674 12.9943 10.3094L12.7423 9.53638C12.6886 9.37853 12.586 9.24191 12.4493 9.14638C12.3473 9.07343 12.2295 9.02549 12.1056 9.00642C11.9816 8.98736 11.8549 8.99772 11.7357 9.03665C11.6164 9.07558 11.508 9.142 11.4192 9.23054C11.3304 9.31909 11.2636 9.42727 11.2243 9.54638L10.9773 10.3084C10.925 10.466 10.8375 10.6097 10.7213 10.7284C10.6066 10.8449 10.4667 10.9335 10.3123 10.9874L9.53931 11.2394C9.38025 11.2933 9.2422 11.3959 9.1447 11.5326C9.04721 11.6694 8.99522 11.8333 8.99611 12.0013C8.99699 12.1692 9.0507 12.3326 9.14963 12.4683C9.24856 12.604 9.38769 12.7051 9.54731 12.7574L10.3103 13.0044C10.4692 13.0578 10.6136 13.1471 10.7323 13.2654C10.8505 13.3836 10.939 13.5283 10.9903 13.6874L11.2433 14.4614C11.2981 14.6178 11.4001 14.7534 11.5353 14.8494ZM10.6223 12.0564L10.4433 11.9974L10.6273 11.9334C10.9291 11.8284 11.2027 11.6556 11.4273 11.4284C11.6537 11.1994 11.8248 10.9216 11.9273 10.6164L11.9853 10.4384L12.0443 10.6194C12.1463 10.9261 12.3185 11.2047 12.5471 11.4332C12.7757 11.6617 13.0545 11.8336 13.3613 11.9354L13.5563 11.9984L13.3763 12.0574C13.0689 12.1596 12.7898 12.3322 12.5611 12.5616C12.3324 12.791 12.1606 13.0707 12.0593 13.3784L12.0003 13.5594L11.9423 13.3784C11.8409 13.0702 11.6687 12.7901 11.4394 12.5605C11.2102 12.3309 10.9303 12.1583 10.6223 12.0564Z"></path></svg>'},33027:b=>{b.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path d="M6 6h4v4H6z"></path><path fill-rule="evenodd" clip-rule="evenodd" d="M8.6 1c1.6.1 3.1.9 4.2 2 1.3 1.4 2 3.1 2 5.1 0 1.6-.6 3.1-1.6 4.4-1 1.2-2.4 2.1-4 2.4-1.6.3-3.2.1-4.6-.7-1.4-.8-2.5-2-3.1-3.5C.9 9.2.8 7.5 1.3 6c.5-1.6 1.4-2.9 2.8-3.8C5.4 1.3 7 .9 8.6 1zm.5 12.9c1.3-.3 2.5-1 3.4-2.1.8-1.1 1.3-2.4 1.2-3.8 0-1.6-.6-3.2-1.7-4.3-1-1-2.2-1.6-3.6-1.7-1.3-.1-2.7.2-3.8 1-1.1.8-1.9 1.9-2.3 3.3-.4 1.3-.4 2.7.2 4 .6 1.3 1.5 2.3 2.7 3 1.2.7 2.6.9 3.9.6z"></path></svg>'},17411:b=>{b.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M2.006 8.267L.78 9.5 0 8.73l2.09-2.07.76.01 2.09 2.12-.76.76-1.167-1.18a5 5 0 0 0 9.4 1.983l.813.597a6 6 0 0 1-11.22-2.683zm10.99-.466L11.76 6.55l-.76.76 2.09 2.11.76.01 2.09-2.07-.75-.76-1.194 1.18a6 6 0 0 0-11.11-2.92l.81.594a5 5 0 0 1 9.3 2.346z"></path></svg>'},65013:b=>{b.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M3.57 6.699l5.693-4.936L8.585 1 3.273 5.596l-1.51-1.832L1 4.442l1.85 2.214.72.043zM15 5H6.824l2.307-2H15v2zM6 7h9v2H6V7zm9 4H6v2h9v-2z"></path></svg>'},2481:b=>{b.exports='<svg viewBox="0 0 16 16" xmlns="http://www.w3.org/2000/svg" fill="currentColor"><path fill-rule="evenodd" clip-rule="evenodd" d="M14 5H2V3h12v2zm0 4H2V7h12v2zM2 13h12v-2H2v2z"></path></svg>'}},ei={};function J(b){var _=ei[b];if(_!==void 0)return _.exports;var B=ei[b]={id:b,exports:{}};return Dl[b].call(B.exports,B,B.exports,J),B.exports}i(J,"__webpack_require__"),J.n=b=>{var _=b&&b.__esModule?()=>b.default:()=>b;return J.d(_,{a:_}),_},J.d=(b,_)=>{for(var B in _)J.o(_,B)&&!J.o(b,B)&&Object.defineProperty(b,B,{enumerable:!0,get:_[B]})},J.o=(b,_)=>Object.prototype.hasOwnProperty.call(b,_),J.nc=void 0;var l1={};(()=>{"use strict";var nt;var b=J(85072),_=J.n(b),B=J(97825),K=J.n(B),V=J(77659),T=J.n(V),g=J(55056),p=J.n(g),D=J(10540),A=J.n(D),$=J(41113),H=J.n($),X=J(2410),Y={};Y.styleTagTransform=H(),Y.setAttributes=p(),Y.insert=T().bind(null,"head"),Y.domAPI=K(),Y.insertStyleElement=A();var Oe=_()(X.A,Y);const He=X.A&&X.A.locals?X.A.locals:void 0;var de=J(3554),De={};De.styleTagTransform=H(),De.setAttributes=p(),De.insert=T().bind(null,"head"),De.domAPI=K(),De.insertStyleElement=A();var tt=_()(de.A,De);const j=de.A&&de.A.locals?de.A.locals:void 0;var N=J(17334),l=J(96540),oe=J(40961),q=(o=>(o[o.Committed=0]="Committed",o[o.Mentioned=1]="Mentioned",o[o.Subscribed=2]="Subscribed",o[o.Commented=3]="Commented",o[o.Reviewed=4]="Reviewed",o[o.NewCommitsSinceReview=5]="NewCommitsSinceReview",o[o.Labeled=6]="Labeled",o[o.Milestoned=7]="Milestoned",o[o.Assigned=8]="Assigned",o[o.Unassigned=9]="Unassigned",o[o.HeadRefDeleted=10]="HeadRefDeleted",o[o.Merged=11]="Merged",o[o.CrossReferenced=12]="CrossReferenced",o[o.Closed=13]="Closed",o[o.Reopened=14]="Reopened",o[o.CopilotStarted=15]="CopilotStarted",o[o.CopilotFinished=16]="CopilotFinished",o[o.CopilotFinishedError=17]="CopilotFinishedError",o[o.Other=18]="Other",o))(q||{}),Z=Object.defineProperty,O=i((o,a,u)=>a in o?Z(o,a,{enumerable:!0,configurable:!0,writable:!0,value:u}):o[a]=u,"__defNormalProp"),I=i((o,a,u)=>O(o,typeof a!="symbol"?a+"":a,u),"__publicField");const ne=acquireVsCodeApi(),it=class it{constructor(a){I(this,"_commandHandler"),I(this,"lastSentReq"),I(this,"pendingReplies"),this._commandHandler=a,this.lastSentReq=0,this.pendingReplies=Object.create(null),window.addEventListener("message",this.handleMessage.bind(this))}registerCommandHandler(a){this._commandHandler=a}async postMessage(a){const u=String(++this.lastSentReq);return new Promise((c,f)=>{this.pendingReplies[u]={resolve:c,reject:f},a=Object.assign(a,{req:u}),ne.postMessage(a)})}handleMessage(a){const u=a.data;if(u.seq){const c=this.pendingReplies[u.seq];if(c){u.err?c.reject(u.err):c.resolve(u.res);return}}this._commandHandler&&this._commandHandler(u.res)}};i(it,"MessageHandler");let G=it;function se(o){return new G(o)}i(se,"getMessageHandler");function fe(){return ne.getState()}i(fe,"getState");function pe(o){const a=fe();a&&a.number&&a.number===(o==null?void 0:o.number)&&(o.pendingCommentText=a.pendingCommentText),o&&ne.setState(o)}i(pe,"setState");function ve(o){const a=ne.getState();ne.setState(Object.assign(a,o))}i(ve,"updateState");var Ae=Object.defineProperty,Ve=i((o,a,u)=>a in o?Ae(o,a,{enumerable:!0,configurable:!0,writable:!0,value:u}):o[a]=u,"context_defNormalProp"),re=i((o,a,u)=>Ve(o,typeof a!="symbol"?a+"":a,u),"context_publicField");const qe=(nt=class{constructor(a=fe(),u=null,c=null){this.pr=a,this.onchange=u,this._handler=c,re(this,"setTitle",async f=>{const h=await this.postMessage({command:"pr.edit-title",args:{text:f}});this.updatePR({titleHTML:h.titleHTML})}),re(this,"setDescription",f=>this.postMessage({command:"pr.edit-description",args:{text:f}})),re(this,"checkout",()=>this.postMessage({command:"pr.checkout"})),re(this,"openChanges",f=>this.postMessage({command:"pr.open-changes",args:{openToTheSide:f}})),re(this,"copyPrLink",()=>this.postMessage({command:"pr.copy-prlink"})),re(this,"copyVscodeDevLink",()=>this.postMessage({command:"pr.copy-vscodedevlink"})),re(this,"cancelCodingAgent",f=>this.postMessage({command:"pr.cancel-coding-agent",args:f})),re(this,"exitReviewMode",async()=>{if(this.pr)return this.postMessage({command:"pr.checkout-default-branch",args:this.pr.repositoryDefaultBranch})}),re(this,"gotoChangesSinceReview",()=>this.postMessage({command:"pr.gotoChangesSinceReview"})),re(this,"refresh",async()=>{this.pr&&(this.pr.busy=!0),this.updatePR(this.pr),await this.postMessage({command:"pr.refresh"}),this.pr&&(this.pr.busy=!1),this.updatePR(this.pr)}),re(this,"checkMergeability",()=>this.postMessage({command:"pr.checkMergeability"})),re(this,"changeEmail",async f=>{const h=await this.postMessage({command:"pr.change-email",args:f});this.updatePR({emailForCommit:h})}),re(this,"merge",async f=>await this.postMessage({command:"pr.merge",args:f})),re(this,"openOnGitHub",()=>this.postMessage({command:"pr.openOnGitHub"})),re(this,"deleteBranch",()=>this.postMessage({command:"pr.deleteBranch"})),re(this,"revert",async()=>{this.updatePR({busy:!0});const f=await this.postMessage({command:"pr.revert"});this.updatePR({busy:!1,...f})}),re(this,"readyForReview",()=>this.postMessage({command:"pr.readyForReview"})),re(this,"addReviewers",()=>this.postMessage({command:"pr.change-reviewers"})),re(this,"changeProjects",()=>this.postMessage({command:"pr.change-projects"})),re(this,"removeProject",f=>this.postMessage({command:"pr.remove-project",args:f})),re(this,"addMilestone",()=>this.postMessage({command:"pr.add-milestone"})),re(this,"removeMilestone",()=>this.postMessage({command:"pr.remove-milestone"})),re(this,"addAssignees",()=>this.postMessage({command:"pr.change-assignees"})),re(this,"addAssigneeYourself",()=>this.postMessage({command:"pr.add-assignee-yourself"})),re(this,"addAssigneeCopilot",()=>this.postMessage({command:"pr.add-assignee-copilot"})),re(this,"addLabels",()=>this.postMessage({command:"pr.add-labels"})),re(this,"create",()=>this.postMessage({command:"pr.open-create"})),re(this,"deleteComment",async f=>{await this.postMessage({command:"pr.delete-comment",args:f});const{pr:h}=this;if(!h)throw new Error("Unexpectedly no PR when trying to delete comment");const{id:y,pullRequestReviewId:C}=f;if(!C){this.updatePR({events:h.events.filter(F=>F.id!==y)});return}const E=h.events.findIndex(F=>F.id===C);if(E===-1){console.error("Could not find review:",C);return}const R=h.events[E];if(!R.comments){console.error("No comments to delete for review:",C,R);return}h.events.splice(E,1,{...R,comments:R.comments.filter(F=>F.id!==y)}),this.updatePR(h)}),re(this,"editComment",f=>this.postMessage({command:"pr.edit-comment",args:f})),re(this,"updateDraft",(f,h)=>{const C=fe().pendingCommentDrafts||Object.create(null);h!==C[f]&&(C[f]=h,this.updatePR({pendingCommentDrafts:C}))}),re(this,"requestChanges",f=>this.submitReviewCommand("pr.request-changes",f)),re(this,"approve",f=>this.submitReviewCommand("pr.approve",f)),re(this,"submit",f=>this.submitReviewCommand("pr.submit",f)),re(this,"close",async f=>{const{pr:h}=this;if(!h)throw new Error("Unexpectedly no PR when trying to close");try{const y=await this.postMessage({command:"pr.close",args:f});let C=[...h.events];y.commentEvent&&C.push(y.commentEvent),y.closeEvent&&C.push(y.closeEvent),this.updatePR({events:C,pendingCommentText:"",state:y.state})}catch{}}),re(this,"removeLabel",async f=>{const{pr:h}=this;if(!h)throw new Error("Unexpectedly no PR when trying to remove label");await this.postMessage({command:"pr.remove-label",args:f});const y=h.labels.filter(C=>C.name!==f);this.updatePR({labels:y})}),re(this,"applyPatch",async f=>{this.postMessage({command:"pr.apply-patch",args:{comment:f}})}),re(this,"reRequestReview",async f=>{const{pr:h}=this;if(!h)throw new Error("Unexpectedly no PR when trying to re-request review");const{reviewers:y}=await this.postMessage({command:"pr.re-request-review",args:f});h.reviewers=y,this.updatePR(h)}),re(this,"updateBranch",async()=>{var f,h;const{pr:y}=this;if(!y)throw new Error("Unexpectedly no PR when trying to update branch");const C=await this.postMessage({command:"pr.update-branch"});y.events=(f=C.events)!=null?f:y.events,y.mergeable=(h=C.mergeable)!=null?h:y.mergeable,this.updatePR(y)}),re(this,"dequeue",async()=>{const{pr:f}=this;if(!f)throw new Error("Unexpectedly no PR when trying to dequeue");await this.postMessage({command:"pr.dequeue"})&&(f.mergeQueueEntry=void 0),this.updatePR(f)}),re(this,"enqueue",async()=>{const{pr:f}=this;if(!f)throw new Error("Unexpectedly no PR when trying to enqueue");const h=await this.postMessage({command:"pr.enqueue"});h.mergeQueueEntry&&(f.mergeQueueEntry=h.mergeQueueEntry),this.updatePR(f)}),re(this,"openDiff",f=>this.postMessage({command:"pr.open-diff",args:{comment:f}})),re(this,"toggleResolveComment",(f,h,y)=>{this.postMessage({command:"pr.resolve-comment-thread",args:{threadId:f,toResolve:y,thread:h}}).then(C=>{C?this.updatePR({events:C}):this.refresh()})}),re(this,"openSessionLog",f=>this.postMessage({command:"pr.open-session-log",args:{link:f}})),re(this,"openCommitChanges",async f=>{this.updatePR({loadingCommit:f});try{const h={commitSha:f};await this.postMessage({command:"pr.openCommitChanges",args:h})}finally{this.updatePR({loadingCommit:void 0})}}),re(this,"setPR",f=>(this.pr=f,pe(this.pr),this.onchange&&this.onchange(this.pr),this)),re(this,"updatePR",f=>(ve(f),this.pr=this.pr?{...this.pr,...f}:f,this.onchange&&this.onchange(this.pr),this)),re(this,"handleMessage",f=>{var h;switch(f.command){case"pr.clear":this.setPR(void 0);return;case"pr.initialize":return this.setPR(f.pullrequest);case"update-state":return this.updatePR({state:f.state});case"pr.update-checkout-status":return this.updatePR({isCurrentlyCheckedOut:f.isCurrentlyCheckedOut});case"pr.deleteBranch":const y={};return f.branchTypes&&f.branchTypes.map(E=>{E==="local"?y.isLocalHeadDeleted=!0:(E==="remote"||E==="upstream")&&(y.isRemoteHeadDeleted=!0)}),this.updatePR(y);case"pr.enable-exit":return this.updatePR({isCurrentlyCheckedOut:!0});case"set-scroll":window.scrollTo(f.scrollPosition.x,f.scrollPosition.y);return;case"pr.scrollToPendingReview":const C=(h=document.getElementById("pending-review"))!=null?h:document.getElementById("comment-textarea");C&&(C.scrollIntoView(),C.focus());return;case"pr.submitting-review":return this.updatePR({busy:!0,lastReviewType:f.lastReviewType});case"pr.append-review":return this.appendReview(f)}}),c||(this._handler=se(this.handleMessage))}async submitReviewCommand(a,u){try{const c=await this.postMessage({command:a,args:u});return this.appendReview(c)}catch{return this.updatePR({busy:!1})}}appendReview(a){const{pr:u}=this;if(!u)throw new Error("Unexpectedly no PR when trying to append review");const{events:c,reviewers:f,reviewedEvent:h}=a;if(u.busy=!1,!c){this.updatePR(u);return}f&&(u.reviewers=f),u.events=c.length===0?[...u.events,h]:c,h.event===q.Reviewed&&(u.currentUserReviewState=h.state),u.pendingCommentText="",u.pendingReviewType=void 0,this.updatePR(u)}async updateAutoMerge({autoMerge:a,autoMergeMethod:u}){const{pr:c}=this;if(!c)throw new Error("Unexpectedly no PR when trying to update auto merge");const f=await this.postMessage({command:"pr.update-automerge",args:{autoMerge:a,autoMergeMethod:u}});c.autoMerge=f.autoMerge,c.autoMergeMethod=f.autoMergeMethod,this.updatePR(c)}postMessage(a){var u,c;return(c=(u=this._handler)==null?void 0:u.postMessage(a))!=null?c:Promise.resolve(void 0)}},i(nt,"_PRContext"),nt);re(qe,"instance",new qe);let at=qe;const xe=(0,l.createContext)(at.instance);var Ue=(o=>(o[o.Query=0]="Query",o[o.All=1]="All",o[o.LocalPullRequest=2]="LocalPullRequest",o))(Ue||{}),z=(o=>(o.Approve="APPROVE",o.RequestChanges="REQUEST_CHANGES",o.Comment="COMMENT",o))(z||{}),Q=(o=>(o.Open="OPEN",o.Merged="MERGED",o.Closed="CLOSED",o))(Q||{}),ue=(o=>(o[o.Mergeable=0]="Mergeable",o[o.NotMergeable=1]="NotMergeable",o[o.Conflict=2]="Conflict",o[o.Unknown=3]="Unknown",o[o.Behind=4]="Behind",o))(ue||{}),w=(o=>(o[o.AwaitingChecks=0]="AwaitingChecks",o[o.Locked=1]="Locked",o[o.Mergeable=2]="Mergeable",o[o.Queued=3]="Queued",o[o.Unmergeable=4]="Unmergeable",o))(w||{}),P=(o=>(o.User="User",o.Organization="Organization",o.Mannequin="Mannequin",o.Bot="Bot",o))(P||{});function he(o){switch(o){case"Organization":return"Organization";case"Mannequin":return"Mannequin";case"Bot":return"Bot";default:return"User"}}i(he,"toAccountType");function ke(o){var a;return $e(o)?o.id:(a=o.specialDisplayName)!=null?a:o.login}i(ke,"reviewerId");function be(o){var a,u,c;return $e(o)?(u=(a=o.name)!=null?a:o.slug)!=null?u:o.id:(c=o.specialDisplayName)!=null?c:o.login}i(be,"reviewerLabel");function $e(o){return"org"in o}i($e,"isTeam");function xt(o){return"isAuthor"in o&&"isCommenter"in o}i(xt,"isSuggestedReviewer");var Le=(o=>(o.Issue="Issue",o.PullRequest="PullRequest",o))(Le||{}),ge=(o=>(o.Success="success",o.Failure="failure",o.Neutral="neutral",o.Pending="pending",o.Unknown="unknown",o))(ge||{}),Ne=(o=>(o.Comment="comment",o.Approve="approve",o.RequestChanges="requestChanges",o))(Ne||{}),Xr=(o=>(o[o.None=0]="None",o[o.Available=1]="Available",o[o.ReviewedWithComments=2]="ReviewedWithComments",o[o.ReviewedWithoutComments=3]="ReviewedWithoutComments",o))(Xr||{});function kt(o){var a,u;const c=(a=o.submittedAt)!=null?a:o.createdAt,f=c&&Date.now()-new Date(c).getTime()<1e3*60,h=(u=o.state)!=null?u:o.event===q.Commented?"COMMENTED":void 0;let y="";if(f)switch(h){case"APPROVED":y="Pull request approved";break;case"CHANGES_REQUESTED":y="Changes requested on pull request";break;case"COMMENTED":y="Commented on pull request";break}return y}i(kt,"ariaAnnouncementForReview");var Jr=J(37007);const ti=new Jr.EventEmitter;function ut(o){const[a,u]=(0,l.useState)(o);return(0,l.useEffect)(()=>{a!==o&&u(o)},[o]),[a,u]}i(ut,"useStateProp");const Ce=i(({className:o="",src:a,title:u})=>l.createElement("span",{className:`icon ${o}`,title:u,dangerouslySetInnerHTML:{__html:a}}),"Icon"),zn=null,Wt=l.createElement(Ce,{src:J(61440)}),Bn=l.createElement(Ce,{src:J(34894),className:"check"}),Er=l.createElement(Ce,{src:J(61779),className:"skip"}),ni=l.createElement(Ce,{src:J(30407)}),ri=l.createElement(Ce,{src:J(10650)}),Al=l.createElement(Ce,{src:J(2301)}),eo=l.createElement(Ce,{src:J(72362)}),kr=l.createElement(Ce,{src:J(5771)}),oi=l.createElement(Ce,{src:J(37165)}),to=l.createElement(Ce,{src:J(46279)}),br=l.createElement(Ce,{src:J(90346)}),Il=l.createElement(Ce,{src:J(44370)}),ii=l.createElement(Ce,{src:J(90659)}),jn=l.createElement(Ce,{src:J(14268)}),Hl=l.createElement(Ce,{src:J(83344)}),Rt=l.createElement(Ce,{src:J(83962)}),no=l.createElement(Ce,{src:J(15010)}),Ot=l.createElement(Ce,{src:J(19443),className:"pending"}),ro=l.createElement(Ce,{src:J(98923)}),Un=l.createElement(Ce,{src:J(15493)}),_t=l.createElement(Ce,{src:J(85130),className:"close"}),li=l.createElement(Ce,{src:J(17411)}),si=l.createElement(Ce,{src:J(30340)}),Fl=l.createElement(Ce,{src:J(9649)}),Vl=l.createElement(Ce,{src:J(92359)}),ai=l.createElement(Ce,{src:J(34439)}),$l=l.createElement(Ce,{src:J(96855)}),ui=l.createElement(Ce,{src:J(5064)}),Ea=l.createElement(Ce,{src:J(20628)}),ci=l.createElement(Ce,{src:J(80459)}),Wn=l.createElement(Ce,{src:J(70596)}),di=l.createElement(Ce,{src:J(33027)}),fi=l.createElement(Ce,{src:J(40027)}),mi=l.createElement(Ce,{src:J(64674)}),pi=l.createElement(Ce,{src:J(12158)}),hi=l.createElement(Ce,{src:J(2481)}),vi=l.createElement(Ce,{src:J(65013)}),oo=l.createElement(Ce,{src:J(93492)}),rn=l.createElement(Ce,{className:"loading",src:J(95570)}),gi=l.createElement(Ce,{className:"copilot-icon",src:J(9336)}),Zn=l.createElement(Ce,{className:"copilot-icon",src:J(94339)}),_r=l.createElement(Ce,{className:"copilot-icon",src:J(58726)});function xn(){const[o,a]=(0,l.useState)([0,0]);return(0,l.useLayoutEffect)(()=>{function u(){a([window.innerWidth,window.innerHeight])}return i(u,"updateSize"),window.addEventListener("resize",u),u(),()=>window.removeEventListener("resize",u)},[]),o}i(xn,"useWindowSize");const qn=i(({optionsContext:o,defaultOptionLabel:a,defaultOptionValue:u,defaultAction:c,allOptions:f,optionsTitle:h,disabled:y,hasSingleAction:C,spreadable:E,isSecondary:R})=>{const[F,W]=(0,l.useState)(!1),ae=i(me=>{me.target instanceof HTMLElement&&me.target.classList.contains("split-right")||W(!1)},"onHideAction");(0,l.useEffect)(()=>{const me=i(Pe=>ae(Pe),"onClickOrKey");F?(document.addEventListener("click",me),document.addEventListener("keydown",me)):(document.removeEventListener("click",me),document.removeEventListener("keydown",me))},[F,W]);const Se=(0,l.useRef)();return xn(),l.createElement("div",{className:`dropdown-container${E?" spreadable":""}`,ref:Se},Se.current&&E&&Se.current.clientWidth>375&&f&&!C?f().map(({label:me,value:Pe,action:we})=>l.createElement("button",{className:"inlined-dropdown",key:Pe,title:me,disabled:y,onClick:we,value:Pe},me)):l.createElement("div",{className:"primary-split-button"},l.createElement("button",{className:`split-left${R?" secondary":""}`,disabled:y,onClick:c,value:u(),title:typeof a()=="string"?a():h},a()),C?null:l.createElement("div",{className:`split${R?" secondary":""}${y?" disabled":""}`},l.createElement("div",{className:`separator${y?" disabled":""}`})),C?null:l.createElement("button",{className:`split-right${R?" secondary":""}`,title:h,disabled:y,"aria-expanded":F,onClick:i(me=>{me.preventDefault();const Pe=me.target.getBoundingClientRect(),we=Pe.left,Te=Pe.bottom;me.target.dispatchEvent(new MouseEvent("contextmenu",{bubbles:!0,clientX:we,clientY:Te})),me.stopPropagation()},"onClick"),onMouseDown:i(()=>W(!0),"onMouseDown"),onKeyDown:i(me=>{(me.key==="Enter"||me.key===" ")&&W(!0)},"onKeyDown"),"data-vscode-context":o()},ri)))},"contextDropdown_ContextDropdown"),rt="\xA0",Lr=i(({children:o})=>{const a=l.Children.count(o);return l.createElement(l.Fragment,{children:l.Children.map(o,(u,c)=>typeof u=="string"?`${c>0?rt:""}${u}${c<a-1&&typeof o[c+1]!="string"?rt:""}`:u)})},"Spaced");var zl=J(57975),yi=J(74353),Qn=J.n(yi),Ci=J(6279),Kn=J.n(Ci),wi=J(53581),io=J.n(wi),on=Object.defineProperty,xi=i((o,a,u)=>a in o?on(o,a,{enumerable:!0,configurable:!0,writable:!0,value:u}):o[a]=u,"lifecycle_defNormalProp"),lo=i((o,a,u)=>xi(o,typeof a!="symbol"?a+"":a,u),"lifecycle_publicField");function Bl(o){return{dispose:o}}i(Bl,"toDisposable");function jl(o){return Bl(()=>ln(o))}i(jl,"lifecycle_combinedDisposable");function ln(o){for(;o.length;){const a=o.pop();a==null||a.dispose()}}i(ln,"disposeAll");function Ei(o,a){return a.push(o),o}i(Ei,"addDisposable");const jt=class jt{constructor(){lo(this,"_isDisposed",!1),lo(this,"_disposables",[])}dispose(){this._isDisposed||(this._isDisposed=!0,ln(this._disposables),this._disposables=[])}_register(a){return this._isDisposed?a.dispose():this._disposables.push(a),a}get isDisposed(){return this._isDisposed}};i(jt,"Disposable");let En=jt;var Ul=Object.defineProperty,Sr=i((o,a,u)=>a in o?Ul(o,a,{enumerable:!0,configurable:!0,writable:!0,value:u}):o[a]=u,"utils_defNormalProp"),Ke=i((o,a,u)=>Sr(o,typeof a!="symbol"?a+"":a,u),"utils_publicField");Qn().extend(Kn(),{thresholds:[{l:"s",r:44,d:"second"},{l:"m",r:89},{l:"mm",r:44,d:"minute"},{l:"h",r:89},{l:"hh",r:21,d:"hour"},{l:"d",r:35},{l:"dd",r:6,d:"day"},{l:"w",r:7},{l:"ww",r:3,d:"week"},{l:"M",r:4},{l:"MM",r:10,d:"month"},{l:"y",r:17},{l:"yy",d:"year"}]}),Qn().extend(io()),Qn().updateLocale("en",{relativeTime:{future:"in %s",past:"%s ago",s:"seconds",m:"a minute",mm:"%d minutes",h:"an hour",hh:"%d hours",d:"a day",dd:"%d days",w:"a week",ww:"%d weeks",M:"a month",MM:"%d months",y:"a year",yy:"%d years"}});function Wl(o,a){const u=Object.create(null);return o.filter(c=>{const f=a(c);return u[f]?!1:(u[f]=!0,!0)})}i(Wl,"uniqBy");function so(...o){return(a,u=null,c)=>{const f=combinedDisposable(o.map(h=>h(y=>a.call(u,y))));return c&&c.push(f),f}}i(so,"anyEvent");function Zl(o,a){return(u,c=null,f)=>o(h=>a(h)&&u.call(c,h),null,f)}i(Zl,"filterEvent");function ql(o){return(a,u=null,c)=>{const f=o(h=>(f.dispose(),a.call(u,h)),null,c);return f}}i(ql,"onceEvent");function ki(o){return/^[a-zA-Z]:\\/.test(o)}i(ki,"isWindowsPath");function bi(o,a,u=sep){return o===a?!0:(o.charAt(o.length-1)!==u&&(o+=u),ki(o)&&(o=o.toLowerCase(),a=a.toLowerCase()),a.startsWith(o))}i(bi,"isDescendant");function ao(o,a){return o.reduce((u,c)=>{const f=a(c);return u[f]=[...u[f]||[],c],u},Object.create(null))}i(ao,"groupBy");const Yt=class Yt extends Error{constructor(a){super(`Unreachable case: ${a}`)}};i(Yt,"UnreachableCaseError");let Yn=Yt;function _i(o){return!!o.errors}i(_i,"isHookError");function uo(o){let a=!0;if(o.errors&&Array.isArray(o.errors)){for(const u of o.errors)if(!u.field||!u.value||!u.status){a=!1;break}}else a=!1;return a}i(uo,"hasFieldErrors");function Vt(o){if(!(o instanceof Error))return typeof o=="string"?o:o.gitErrorCode?`${o.message}. Please check git output for more details`:o.stderr?`${o.stderr}. Please check git output for more details`:"Error";let a=o.message,u;if(o.message==="Validation Failed"&&uo(o))u=o.errors.map(c=>`Value "${c.value}" cannot be set for field ${c.field} (code: ${c.status})`).join(", ");else{if(o.message.startsWith("Validation Failed:"))return o.message;if(_i(o)&&o.errors)return o.errors.map(c=>typeof c=="string"?c:c.message).join(", ")}return u&&(a=`${a}: ${u}`),a}i(Vt,"formatError");async function sn(o){return new Promise(a=>{const u=o(c=>{u.dispose(),a(c)})})}i(sn,"asPromise");async function an(o,a){return Promise.race([o,new Promise(u=>{setTimeout(()=>u(void 0),a)})])}i(an,"promiseWithTimeout");function Lt(o){const a=Qn()(o),u=Date.now();return a.diff(u,"month"),a.diff(u,"month")<1?a.fromNow():a.diff(u,"year")<1?`on ${a.format("MMM D")}`:`on ${a.format("MMM D, YYYY")}`}i(Lt,"dateFromNow");function un(o,a,u=!1){o.startsWith("#")&&(o=o.substring(1));const c=bn(o);if(a){const f=co(c.r,c.g,c.b),h=.6,y=.18,C=.3,E=(c.r*.2126+c.g*.7152+c.b*.0722)/255,R=Math.max(0,Math.min((E-h)*-1e3,1)),F=(h-E)*100*R,W=bn(Li(f.h,f.s,f.l+F)),ae=`#${Li(f.h,f.s,f.l+F)}`,Se=u?`#${kn({...c,a:y})}`:`rgba(${c.r},${c.g},${c.b},${y})`,me=u?`#${kn({...W,a:C})}`:`rgba(${W.r},${W.g},${W.b},${C})`;return{textColor:ae,backgroundColor:Se,borderColor:me}}else return{textColor:`#${Ql(c)}`,backgroundColor:`#${o}`,borderColor:`#${o}`}}i(un,"utils_gitHubLabelColor");const kn=i(o=>{const a=[o.r,o.g,o.b];return o.a&&a.push(Math.floor(o.a*255)),a.map(u=>u.toString(16).padStart(2,"0")).join("")},"rgbToHex");function bn(o){const a=/^([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(o);return a?{r:parseInt(a[1],16),g:parseInt(a[2],16),b:parseInt(a[3],16)}:{r:0,g:0,b:0}}i(bn,"hexToRgb");function co(o,a,u){o/=255,a/=255,u/=255;let c=Math.min(o,a,u),f=Math.max(o,a,u),h=f-c,y=0,C=0,E=0;return h==0?y=0:f==o?y=(a-u)/h%6:f==a?y=(u-o)/h+2:y=(o-a)/h+4,y=Math.round(y*60),y<0&&(y+=360),E=(f+c)/2,C=h==0?0:h/(1-Math.abs(2*E-1)),C=+(C*100).toFixed(1),E=+(E*100).toFixed(1),{h:y,s:C,l:E}}i(co,"rgbToHsl");function Li(o,a,u){const c=u/100,f=a*Math.min(c,1-c)/100,h=i(y=>{const C=(y+o/30)%12,E=c-f*Math.max(Math.min(C-3,9-C,1),-1);return Math.round(255*E).toString(16).padStart(2,"0")},"f");return`${h(0)}${h(8)}${h(4)}`}i(Li,"hslToHex");function Ql(o){return(.299*o.r+.587*o.g+.114*o.b)/255>.5?"000000":"ffffff"}i(Ql,"contrastColor");var fo=(o=>(o[o.Period=46]="Period",o[o.Slash=47]="Slash",o[o.A=65]="A",o[o.Z=90]="Z",o[o.Backslash=92]="Backslash",o[o.a=97]="a",o[o.z=122]="z",o))(fo||{});function mo(o,a){return o<a?-1:o>a?1:0}i(mo,"compare");function cn(o,a,u=0,c=o.length,f=0,h=a.length){for(;u<c&&f<h;u++,f++){const E=o.charCodeAt(u),R=a.charCodeAt(f);if(E<R)return-1;if(E>R)return 1}const y=c-u,C=h-f;return y<C?-1:y>C?1:0}i(cn,"compareSubstring");function Si(o,a){return po(o,a,0,o.length,0,a.length)}i(Si,"compareIgnoreCase");function po(o,a,u=0,c=o.length,f=0,h=a.length){for(;u<c&&f<h;u++,f++){let E=o.charCodeAt(u),R=a.charCodeAt(f);if(E===R)continue;const F=E-R;if(!(F===32&&ho(R))&&!(F===-32&&ho(E)))return Gn(E)&&Gn(R)?F:cn(o.toLowerCase(),a.toLowerCase(),u,c,f,h)}const y=c-u,C=h-f;return y<C?-1:y>C?1:0}i(po,"compareSubstringIgnoreCase");function Gn(o){return o>=97&&o<=122}i(Gn,"isLowerAsciiLetter");function ho(o){return o>=65&&o<=90}i(ho,"isUpperAsciiLetter");const dt=class dt{constructor(){Ke(this,"_value",""),Ke(this,"_pos",0)}reset(a){return this._value=a,this._pos=0,this}next(){return this._pos+=1,this}hasNext(){return this._pos<this._value.length-1}cmp(a){const u=a.charCodeAt(0),c=this._value.charCodeAt(this._pos);return u-c}value(){return this._value[this._pos]}};i(dt,"StringIterator");let vo=dt;const Nn=class Nn{constructor(a=!0){this._caseSensitive=a,Ke(this,"_value"),Ke(this,"_from"),Ke(this,"_to")}reset(a){return this._value=a,this._from=0,this._to=0,this.next()}hasNext(){return this._to<this._value.length}next(){this._from=this._to;let a=!0;for(;this._to<this._value.length;this._to++)if(this._value.charCodeAt(this._to)===46)if(a)this._from++;else break;else a=!1;return this}cmp(a){return this._caseSensitive?cn(a,this._value,0,a.length,this._from,this._to):po(a,this._value,0,a.length,this._from,this._to)}value(){return this._value.substring(this._from,this._to)}};i(Nn,"ConfigKeysIterator");let dn=Nn;const zr=class zr{constructor(a=!0,u=!0){this._splitOnBackslash=a,this._caseSensitive=u,Ke(this,"_value"),Ke(this,"_from"),Ke(this,"_to")}reset(a){return this._value=a.replace(/\\$|\/$/,""),this._from=0,this._to=0,this.next()}hasNext(){return this._to<this._value.length}next(){this._from=this._to;let a=!0;for(;this._to<this._value.length;this._to++){const u=this._value.charCodeAt(this._to);if(u===47||this._splitOnBackslash&&u===92)if(a)this._from++;else break;else a=!1}return this}cmp(a){return this._caseSensitive?cn(a,this._value,0,a.length,this._from,this._to):po(a,this._value,0,a.length,this._from,this._to)}value(){return this._value.substring(this._from,this._to)}};i(zr,"PathIterator");let Xn=zr;var Ti=(o=>(o[o.Scheme=1]="Scheme",o[o.Authority=2]="Authority",o[o.Path=3]="Path",o[o.Query=4]="Query",o[o.Fragment=5]="Fragment",o))(Ti||{});const Br=class Br{constructor(a){this._ignorePathCasing=a,Ke(this,"_pathIterator"),Ke(this,"_value"),Ke(this,"_states",[]),Ke(this,"_stateIdx",0)}reset(a){return this._value=a,this._states=[],this._value.scheme&&this._states.push(1),this._value.authority&&this._states.push(2),this._value.path&&(this._pathIterator=new Xn(!1,!this._ignorePathCasing(a)),this._pathIterator.reset(a.path),this._pathIterator.value()&&this._states.push(3)),this._value.query&&this._states.push(4),this._value.fragment&&this._states.push(5),this._stateIdx=0,this}next(){return this._states[this._stateIdx]===3&&this._pathIterator.hasNext()?this._pathIterator.next():this._stateIdx+=1,this}hasNext(){return this._states[this._stateIdx]===3&&this._pathIterator.hasNext()||this._stateIdx<this._states.length-1}cmp(a){if(this._states[this._stateIdx]===1)return Si(a,this._value.scheme);if(this._states[this._stateIdx]===2)return Si(a,this._value.authority);if(this._states[this._stateIdx]===3)return this._pathIterator.cmp(a);if(this._states[this._stateIdx]===4)return mo(a,this._value.query);if(this._states[this._stateIdx]===5)return mo(a,this._value.fragment);throw new Error}value(){if(this._states[this._stateIdx]===1)return this._value.scheme;if(this._states[this._stateIdx]===2)return this._value.authority;if(this._states[this._stateIdx]===3)return this._pathIterator.value();if(this._states[this._stateIdx]===4)return this._value.query;if(this._states[this._stateIdx]===5)return this._value.fragment;throw new Error}};i(Br,"UriIterator");let Tr=Br;function Mi(o){const u=o.extensionUri.path,c=u.lastIndexOf(".");return c===-1?!1:u.substr(c+1).length>1}i(Mi,"isPreRelease");const Pn=class Pn{constructor(){Ke(this,"segment"),Ke(this,"value"),Ke(this,"key"),Ke(this,"left"),Ke(this,"mid"),Ke(this,"right")}isEmpty(){return!this.left&&!this.mid&&!this.right&&!this.value}};i(Pn,"TernarySearchTreeNode");let _n=Pn;const Gt=class Gt{constructor(a){Ke(this,"_iter"),Ke(this,"_root"),this._iter=a}static forUris(a=()=>!1){return new Gt(new Tr(a))}static forPaths(){return new Gt(new Xn)}static forStrings(){return new Gt(new vo)}static forConfigKeys(){return new Gt(new dn)}clear(){this._root=void 0}set(a,u){const c=this._iter.reset(a);let f;for(this._root||(this._root=new _n,this._root.segment=c.value()),f=this._root;;){const y=c.cmp(f.segment);if(y>0)f.left||(f.left=new _n,f.left.segment=c.value()),f=f.left;else if(y<0)f.right||(f.right=new _n,f.right.segment=c.value()),f=f.right;else if(c.hasNext())c.next(),f.mid||(f.mid=new _n,f.mid.segment=c.value()),f=f.mid;else break}const h=f.value;return f.value=u,f.key=a,h}get(a){var u;return(u=this._getNode(a))==null?void 0:u.value}_getNode(a){const u=this._iter.reset(a);let c=this._root;for(;c;){const f=u.cmp(c.segment);if(f>0)c=c.left;else if(f<0)c=c.right;else if(u.hasNext())u.next(),c=c.mid;else break}return c}has(a){const u=this._getNode(a);return!((u==null?void 0:u.value)===void 0&&(u==null?void 0:u.mid)===void 0)}delete(a){return this._delete(a,!1)}deleteSuperstr(a){return this._delete(a,!0)}_delete(a,u){const c=this._iter.reset(a),f=[];let h=this._root;for(;h;){const y=c.cmp(h.segment);if(y>0)f.push([1,h]),h=h.left;else if(y<0)f.push([-1,h]),h=h.right;else if(c.hasNext())c.next(),f.push([0,h]),h=h.mid;else{for(u?(h.left=void 0,h.mid=void 0,h.right=void 0):h.value=void 0;f.length>0&&h.isEmpty();){let[C,E]=f.pop();switch(C){case 1:E.left=void 0;break;case 0:E.mid=void 0;break;case-1:E.right=void 0;break}h=E}break}}}findSubstr(a){const u=this._iter.reset(a);let c=this._root,f;for(;c;){const h=u.cmp(c.segment);if(h>0)c=c.left;else if(h<0)c=c.right;else if(u.hasNext())u.next(),f=c.value||f,c=c.mid;else break}return c&&c.value||f}findSuperstr(a){const u=this._iter.reset(a);let c=this._root;for(;c;){const f=u.cmp(c.segment);if(f>0)c=c.left;else if(f<0)c=c.right;else if(u.hasNext())u.next(),c=c.mid;else return c.mid?this._entries(c.mid):void 0}}forEach(a){for(const[u,c]of this)a(c,u)}*[Symbol.iterator](){yield*this._entries(this._root)}*_entries(a){a&&(yield*this._entries(a.left),a.value&&(yield[a.key,a.value]),yield*this._entries(a.mid),yield*this._entries(a.right))}};i(Gt,"TernarySearchTree");let Mr=Gt;async function Kl(o,a,u){const c=[];o.replace(a,(y,...C)=>{const E=u(y,...C);return c.push(E),""});const f=await Promise.all(c);let h=0;return o.replace(a,()=>f[h++])}i(Kl,"stringReplaceAsync");async function Ni(o,a,u){const c=Math.ceil(o.length/a);for(let f=0;f<c;f++){const h=o.slice(f*a,(f+1)*a);await Promise.all(h.map(u))}}i(Ni,"batchPromiseAll");function ka(o){return o.replace(/[.*+?^${}()|[\]\\]/g,"\\$&")}i(ka,"escapeRegExp");const St=i(({date:o,href:a})=>{const[u,c]=(0,l.useState)(Lt(o)),f=typeof o=="string"?new Date(o).toLocaleString():o.toLocaleString();return(0,l.useEffect)(()=>{c(Lt(o));const y=i(()=>{const W=Date.now(),ae=typeof o=="string"?new Date(o).getTime():o.getTime(),Se=(W-ae)/(1e3*60);return Se<1?2e4:Se<60?2*6e4:Se<60*24?10*6e4:null},"getUpdateInterval")();if(y===null)return;let C;const E=i(()=>{document.visibilityState==="visible"&&c(Lt(o))},"updateTimeString"),R=i(()=>{C=window.setInterval(E,y)},"startInterval"),F=i(()=>{document.visibilityState==="visible"?(c(Lt(o)),C&&clearInterval(C),R()):C&&clearInterval(C)},"handleVisibilityChange");return R(),document.addEventListener("visibilitychange",F),()=>{C&&clearInterval(C),document.removeEventListener("visibilitychange",F)}},[o]),a?l.createElement("a",{href:a,className:"timestamp",title:f},u):l.createElement("div",{className:"timestamp",title:f},u)},"Timestamp"),go=null,Qe=i(({for:o})=>l.createElement(l.Fragment,null,o.avatarUrl?l.createElement("img",{className:"avatar",src:o.avatarUrl,alt:"",role:"presentation"}):l.createElement(Ce,{className:"avatar-icon",src:J(38440)})),"InnerAvatar"),ct=i(({for:o,link:a=!0,substituteIcon:u})=>a?l.createElement("a",{className:"avatar-link",href:o.url,title:o.url},u!=null?u:l.createElement(Qe,{for:o})):u!=null?u:l.createElement(Qe,{for:o}),"Avatar"),ht=i(({for:o,text:a=be(o)})=>l.createElement("a",{className:"author-link",href:o.url,"aria-label":a,title:o.url},a),"AuthorLink"),Yl=i(({authorAssociation:o},a=u=>`(${u.toLowerCase()})`)=>o.toLowerCase()==="user"?a("you"):o&&o!=="NONE"?a(o):null,"association");function fn(o){const{isPRDescription:a,children:u,comment:c,headerInEditMode:f}=o,{bodyHTML:h,body:y}=c,C="id"in c?c.id:-1,E="canEdit"in c?c.canEdit:!1,R="canDelete"in c?c.canDelete:!1,F=c.pullRequestReviewId,[W,ae]=ut(y),[Se,me]=ut(h),{deleteComment:Pe,editComment:we,setDescription:Te,pr:Ze}=(0,l.useContext)(xe),Be=Ze.pendingCommentDrafts&&Ze.pendingCommentDrafts[C],[Ye,Xe]=(0,l.useState)(!!Be),[_e,lt]=(0,l.useState)(!1);if(Ye)return l.cloneElement(f?l.createElement(yo,{for:c}):l.createElement(l.Fragment,null),{},[l.createElement(Xl,{id:C,key:`editComment${C}`,body:Be||W,onCancel:i(()=>{Ze.pendingCommentDrafts&&delete Ze.pendingCommentDrafts[C],Xe(!1)},"onCancel"),onSave:i(async ft=>{try{const ze=a?await Te(ft):await we({comment:c,text:ft});me(ze.bodyHTML),ae(ft)}finally{Xe(!1)}},"onSave")})]);const At=c.event===q.Commented||c.event===q.Reviewed?kt(c):void 0;return l.createElement(yo,{for:c,onMouseEnter:i(()=>lt(!0),"onMouseEnter"),onMouseLeave:i(()=>lt(!1),"onMouseLeave"),onFocus:i(()=>lt(!0),"onFocus")},At?l.createElement("div",{role:"alert","aria-label":At}):null,l.createElement("div",{className:"action-bar comment-actions",style:{display:_e?"flex":"none"}},l.createElement("button",{title:"Quote reply",className:"icon-button",onClick:i(()=>ti.emit("quoteReply",W),"onClick")},eo),E?l.createElement("button",{title:"Edit comment",className:"icon-button",onClick:i(()=>Xe(!0),"onClick")},Rt):null,R?l.createElement("button",{title:"Delete comment",className:"icon-button",onClick:i(()=>Pe({id:C,pullRequestReviewId:F}),"onClick")},to):null),l.createElement(Co,{comment:c,bodyHTML:Se,body:W,canApplyPatch:Ze.isCurrentlyCheckedOut,allowEmpty:!!o.allowEmpty,specialDisplayBodyPostfix:c.specialDisplayBodyPostfix}),u)}i(fn,"CommentView");function Ln(o){return o.authorAssociation!==void 0}i(Ln,"isReviewEvent");function Jn(o){return o&&typeof o=="object"&&typeof o.body=="string"&&typeof o.diffHunk=="string"}i(Jn,"isIComment");const Gl={PENDING:"will review",COMMENTED:"reviewed",CHANGES_REQUESTED:"requested changes",APPROVED:"approved"},Pi=i(o=>Gl[o]||"reviewed","reviewDescriptor");function yo({for:o,onFocus:a,onMouseEnter:u,onMouseLeave:c,children:f}){var h,y;const C="htmlUrl"in o?o.htmlUrl:o.url,E=(y=Jn(o)&&o.isDraft)!=null?y:Ln(o)&&((h=o.state)==null?void 0:h.toLocaleUpperCase())==="PENDING",R="user"in o?o.user:o.author,F="createdAt"in o?o.createdAt:o.submittedAt;return l.createElement("div",{className:"comment-container comment review-comment",onFocus:a,onMouseEnter:u,onMouseLeave:c},l.createElement("div",{className:"review-comment-container"},l.createElement("h3",{className:`review-comment-header${Ln(o)&&o.comments.length>0?"":" no-details"}`},l.createElement(Lr,null,l.createElement(ct,{for:R}),l.createElement(ht,{for:R}),Ln(o)?Yl(o):null,F?l.createElement(l.Fragment,null,Ln(o)&&o.state?Pi(o.state):"commented",rt,l.createElement(St,{href:C,date:F})):l.createElement("em",null,"pending"),E?l.createElement(l.Fragment,null,l.createElement("span",{className:"pending-label"},"Pending")):null)),f))}i(yo,"CommentBox");function Xl({id:o,body:a,onCancel:u,onSave:c}){const{updateDraft:f}=(0,l.useContext)(xe),h=(0,l.useRef)({body:a,dirty:!1}),y=(0,l.useRef)();(0,l.useEffect)(()=>{const W=setInterval(()=>{h.current.dirty&&(f(o,h.current.body),h.current.dirty=!1)},500);return()=>clearInterval(W)},[h]);const C=(0,l.useCallback)(async()=>{const{markdown:W,submitButton:ae}=y.current;ae.disabled=!0;try{await c(W.value)}finally{ae.disabled=!1}},[y,c]),E=(0,l.useCallback)(W=>{W.preventDefault(),C()},[C]),R=(0,l.useCallback)(W=>{(W.metaKey||W.ctrlKey)&&W.key==="Enter"&&(W.preventDefault(),C())},[C]),F=(0,l.useCallback)(W=>{h.current.body=W.target.value,h.current.dirty=!0},[h]);return l.createElement("form",{ref:y,onSubmit:E},l.createElement("textarea",{name:"markdown",defaultValue:a,onKeyDown:R,onInput:F}),l.createElement("div",{className:"form-actions"},l.createElement("button",{className:"secondary",onClick:u},"Cancel"),l.createElement("button",{type:"submit",name:"submitButton"},"Save")))}i(Xl,"EditComment");const Co=i(({comment:o,bodyHTML:a,body:u,canApplyPatch:c,allowEmpty:f,specialDisplayBodyPostfix:h})=>{var y,C;if(!u&&!a)return f?null:l.createElement("div",{className:"comment-body"},l.createElement("em",null,"No description provided."));const{applyPatch:E}=(0,l.useContext)(xe),R=l.createElement("div",{dangerouslySetInnerHTML:{__html:a!=null?a:""}}),W=((C=(y=u||a)==null?void 0:y.indexOf("```diff"))!=null?C:-1)>-1&&c&&o?l.createElement("button",{onClick:i(()=>E(o),"onClick")},"Apply Patch"):l.createElement(l.Fragment,null);return l.createElement("div",{className:"comment-body"},R,W,h?l.createElement("br",null):null,h?l.createElement("em",null,h):null,l.createElement(wo,{reactions:o==null?void 0:o.reactions}))},"CommentBody"),wo=i(({reactions:o})=>{if(!Array.isArray(o)||o.length===0)return null;const a=o.filter(u=>u.count>0);return a.length===0?null:l.createElement("div",{className:"comment-reactions",style:{marginTop:6}},a.map((u,c)=>{const h=u.reactors||[],y=h.slice(0,10),C=h.length>10?h.length-10:0;let E="";return y.length>0&&(C>0?E=`${Eo(y)} and ${C} more reacted with ${u.label}`:E=`${Eo(y)} reacted with ${u.label}`),l.createElement("div",{key:u.label+c,title:E},l.createElement("span",{className:"reaction-label"},u.label),rt,u.count>1?l.createElement("span",{className:"reaction-count"},u.count):null)}))},"CommentReactions");function Ri({pendingCommentText:o,isCopilotOnMyBehalf:a,state:u,hasWritePermission:c,isIssue:f,isAuthor:h,continueOnGitHub:y,currentUserReviewState:C,lastReviewType:E,busy:R}){const{updatePR:F,requestChanges:W,approve:ae,close:Se,openOnGitHub:me,submit:Pe}=(0,l.useContext)(xe),[we,Te]=(0,l.useState)(!1),Ze=(0,l.useRef)(),Be=(0,l.useRef)();ti.addListener("quoteReply",ze=>{var yt,Wr;const zo=ze.replace(/\n/g,`
> `);F({pendingCommentText:`> ${zo} 

`}),(yt=Be.current)==null||yt.scrollIntoView(),(Wr=Be.current)==null||Wr.focus()});const Ye=i(ze=>{ze.preventDefault();const{value:yt}=Be.current;Se(yt)},"closeButton");let Xe=E!=null?E:C==="APPROVED"?Ne.Approve:C==="CHANGES_REQUESTED"?Ne.RequestChanges:Ne.Comment;async function _e(ze){const{value:yt}=Be.current;if(y&&ze!==Ne.Comment){await me();return}switch(Te(!0),ze){case Ne.RequestChanges:await W(yt);break;case Ne.Approve:await ae(yt);break;default:await Pe(yt)}Te(!1)}i(_e,"submitAction");const lt=(0,l.useCallback)(ze=>{(ze.metaKey||ze.ctrlKey)&&ze.key==="Enter"&&_e(Xe)},[Pe]);async function At(){await _e(Xe)}i(At,"defaultSubmitAction");const ft=h?{[Ne.Comment]:"Comment"}:y?{[Ne.Comment]:"Comment",[Ne.Approve]:"Approve on github.com",[Ne.RequestChanges]:"Request changes on github.com"}:Dt(f);return l.createElement("form",{id:"comment-form",ref:Ze,className:"comment-form main-comment-form",onSubmit:i(()=>{var ze,yt;return Pe((yt=(ze=Be.current)==null?void 0:ze.value)!=null?yt:"")},"onSubmit")},l.createElement("textarea",{id:"comment-textarea",name:"body",ref:Be,onInput:i(({target:ze})=>F({pendingCommentText:ze.value}),"onInput"),onKeyDown:lt,value:o,placeholder:"Leave a comment",onClick:i(()=>{var ze;!o&&a&&!((ze=Be.current)!=null&&ze.textContent)&&(Be.current.textContent="@copilot ",Be.current.setSelectionRange(9,9))},"onClick")}),l.createElement("div",{className:"form-actions"},c||h?l.createElement("button",{id:"close",className:"secondary",disabled:we||u!==Q.Open,onClick:Ye,"data-command":"close"},f?"Close Issue":"Close Pull Request"):null,l.createElement(qn,{optionsContext:i(()=>xo(ft,o),"optionsContext"),defaultAction:At,defaultOptionLabel:i(()=>ft[Xe],"defaultOptionLabel"),defaultOptionValue:i(()=>Xe,"defaultOptionValue"),allOptions:i(()=>{const ze=[];return ft.approve&&ze.push({label:ft[Ne.Approve],value:Ne.Approve,action:i(()=>_e(Ne.Approve),"action")}),ft.comment&&ze.push({label:ft[Ne.Comment],value:Ne.Comment,action:i(()=>_e(Ne.Comment),"action")}),ft.requestChanges&&ze.push({label:ft[Ne.RequestChanges],value:Ne.RequestChanges,action:i(()=>_e(Ne.RequestChanges),"action")}),ze},"allOptions"),optionsTitle:"Submit pull request review",disabled:we||R,hasSingleAction:Object.keys(ft).length===1,spreadable:!0})))}i(Ri,"AddComment");function Dt(o){return o?er:tr}i(Dt,"commentMethods");const er={comment:"Comment"},tr={...er,approve:"Approve",requestChanges:"Request Changes"},xo=i((o,a)=>{const u={preventDefaultContextMenuItems:!0,"github:reviewCommentMenu":!0};return o.approve&&(o.approve===tr.approve?u["github:reviewCommentApprove"]=!0:u["github:reviewCommentApproveOnDotCom"]=!0),o.comment&&(u["github:reviewCommentComment"]=!0),o.requestChanges&&(o.requestChanges===tr.requestChanges?u["github:reviewCommentRequestChanges"]=!0:u["github:reviewCommentRequestChangesOnDotCom"]=!0),u.body=a!=null?a:"",JSON.stringify(u)},"makeCommentMenuContext"),Jl=i(o=>{var a,u;const{updatePR:c,requestChanges:f,approve:h,submit:y,openOnGitHub:C}=useContext(PullRequestContext),[E,R]=useState(!1),F=useRef();let W=(a=o.lastReviewType)!=null?a:o.currentUserReviewState==="APPROVED"?ReviewType.Approve:o.currentUserReviewState==="CHANGES_REQUESTED"?ReviewType.RequestChanges:ReviewType.Comment;async function ae(Te){const{value:Ze}=F.current;if(o.continueOnGitHub&&Te!==ReviewType.Comment){await C();return}switch(R(!0),Te){case ReviewType.RequestChanges:await f(Ze);break;case ReviewType.Approve:await h(Ze);break;default:await y(Ze)}R(!1)}i(ae,"submitAction");async function Se(){await ae(W)}i(Se,"defaultSubmitAction");const me=i(Te=>{c({pendingCommentText:Te.target.value})},"onChangeTextarea"),Pe=useCallback(Te=>{(Te.metaKey||Te.ctrlKey)&&Te.key==="Enter"&&(Te.preventDefault(),Se())},[ae]),we=o.isAuthor?{comment:"Comment"}:o.continueOnGitHub?{comment:"Comment",approve:"Approve on github.com",requestChanges:"Request changes on github.com"}:Dt(o.isIssue);return React.createElement("span",{className:"comment-form"},React.createElement("textarea",{id:"comment-textarea",name:"body",placeholder:"Leave a comment",ref:F,value:(u=o.pendingCommentText)!=null?u:"",onChange:me,onKeyDown:Pe,disabled:E||o.busy}),React.createElement("div",{className:"comment-button"},React.createElement(ContextDropdown,{optionsContext:i(()=>xo(we,o.pendingCommentText),"optionsContext"),defaultAction:Se,defaultOptionLabel:i(()=>we[W],"defaultOptionLabel"),defaultOptionValue:i(()=>W,"defaultOptionValue"),allOptions:i(()=>{const Te=[];return we.approve&&Te.push({label:we[ReviewType.Approve],value:ReviewType.Approve,action:i(()=>ae(ReviewType.Approve),"action")}),we.comment&&Te.push({label:we[ReviewType.Comment],value:ReviewType.Comment,action:i(()=>ae(ReviewType.Comment),"action")}),we.requestChanges&&Te.push({label:we[ReviewType.RequestChanges],value:ReviewType.RequestChanges,action:i(()=>ae(ReviewType.RequestChanges),"action")}),Te},"allOptions"),optionsTitle:"Submit pull request review",disabled:E||o.busy,hasSingleAction:Object.keys(we).length===1,spreadable:!0})))},"AddCommentSimple");function Eo(o){return o.length===0?"":o.length===1?o[0]:o.length===2?`${o[0]} and ${o[1]}`:`${o.slice(0,-1).join(", ")} and ${o[o.length-1]}`}i(Eo,"joinWithAnd");const Oi=["copilot-pull-request-reviewer","copilot-swe-agent","Copilot"];var mn=(o=>(o[o.None=0]="None",o[o.Started=1]="Started",o[o.Completed=2]="Completed",o[o.Failed=3]="Failed",o))(mn||{});function Nr(o){if(!o)return 0;switch(o.event){case q.CopilotStarted:return 1;case q.CopilotFinished:return 2;case q.CopilotFinishedError:return 3;default:return 0}}i(Nr,"copilotEventToStatus");function Di(o){for(let a=o.length-1;a>=0;a--)if(Nr(o[a])!==0)return o[a]}i(Di,"mostRecentCopilotEvent");function ko({canEdit:o,state:a,head:u,base:c,title:f,titleHTML:h,number:y,url:C,author:E,isCurrentlyCheckedOut:R,isDraft:F,isIssue:W,repositoryDefaultBranch:ae,events:Se,owner:me,repo:Pe,busy:we}){const[Te,Ze]=ut(f),[Be,Ye]=(0,l.useState)(!1),Xe=Di(Se);return l.createElement(l.Fragment,null,l.createElement(bo,{title:Te,titleHTML:h,number:y,url:C,inEditMode:Be,setEditMode:Ye,setCurrentTitle:Ze,canEdit:o,owner:me,repo:Pe}),l.createElement(Ai,{state:a,head:u,base:c,author:E,isIssue:W,isDraft:F,codingAgentEvent:Xe}),l.createElement("div",{className:"header-actions"},l.createElement(_o,{isCurrentlyCheckedOut:R,isIssue:W,repositoryDefaultBranch:ae,owner:me,repo:Pe,number:y,busy:we}),l.createElement(Lo,{canEdit:o,codingAgentEvent:Xe})))}i(ko,"Header");function bo({title:o,titleHTML:a,number:u,url:c,inEditMode:f,setEditMode:h,setCurrentTitle:y,canEdit:C,owner:E,repo:R}){const{setTitle:F}=(0,l.useContext)(xe),W=l.createElement("form",{className:"editing-form title-editing-form",onSubmit:i(async Pe=>{Pe.preventDefault();try{const we=Pe.target[0].value;await F(we),y(we)}finally{h(!1)}},"onSubmit")},l.createElement("input",{type:"text",style:{width:"100%"},defaultValue:o}),l.createElement("div",{className:"form-actions"},l.createElement("button",{type:"button",className:"secondary",onClick:i(()=>h(!1),"onClick")},"Cancel"),l.createElement("button",{type:"submit"},"Update"))),ae={preventDefaultContextMenuItems:!0,owner:E,repo:R,number:u};ae["github:copyMenu"]=!0;const Se=l.createElement("div",{className:"overview-title"},l.createElement("h2",null,l.createElement("span",{dangerouslySetInnerHTML:{__html:a}})," ",l.createElement("a",{href:c,title:c,"data-vscode-context":JSON.stringify(ae)},"#",u)),C?l.createElement("button",{title:"Rename",onClick:h,className:"icon-button"},Rt):null);return f?W:Se}i(bo,"Title");function _o({isCurrentlyCheckedOut:o,isIssue:a,repositoryDefaultBranch:u,owner:c,repo:f,number:h,busy:y}){const{refresh:C}=(0,l.useContext)(xe);return l.createElement("div",{className:"button-group"},l.createElement(So,{isCurrentlyCheckedOut:o,isIssue:a,repositoryDefaultBranch:u,owner:c,repo:f,number:h}),l.createElement("button",{title:"Refresh with the latest data from GitHub",onClick:C,className:"secondary"},"Refresh"),y?l.createElement("div",{className:"spinner"},rn):null)}i(_o,"ButtonGroup");function Lo({canEdit:o,codingAgentEvent:a}){const{cancelCodingAgent:u,updatePR:c,openSessionLog:f}=(0,l.useContext)(xe),[h,y]=(0,l.useState)(!1),C=i(async()=>{if(!a)return;y(!0);const W=await u(a);W.events.length>0&&c(W),y(!1)},"cancel"),E=a==null?void 0:a.sessionLink;if(!a||Nr(a)!==mn.Started)return null;const R={preventDefaultContextMenuItems:!0,...E};R["github:codingAgentMenu"]=!0;const F=[];return E&&F.push({label:"View Session",value:"",action:i(()=>f(E),"action")}),o&&F.unshift({label:"Cancel Coding Agent",value:"",action:C}),l.createElement(qn,{optionsContext:i(()=>JSON.stringify(R),"optionsContext"),defaultAction:F[0].action,defaultOptionLabel:i(()=>h?l.createElement(l.Fragment,null,l.createElement("span",{className:"loading-button"},rn),F[0].label):F[0].label,"defaultOptionLabel"),defaultOptionValue:i(()=>F[0].value,"defaultOptionValue"),allOptions:i(()=>F,"allOptions"),optionsTitle:F[0].label,disabled:h,hasSingleAction:!1,spreadable:!1,isSecondary:!0})}i(Lo,"CancelCodingAgentButton");function Ai({state:o,isDraft:a,isIssue:u,author:c,base:f,head:h,codingAgentEvent:y}){const{text:C,color:E,icon:R}=To(o,a,u),F=Nr(y);let W;return F===mn.Started?W=_r:F===mn.Completed?W=gi:F===mn.Failed&&(W=Zn),l.createElement("div",{className:"subtitle"},l.createElement("div",{id:"status",className:`status-badge-${E}`},l.createElement("span",{className:"icon"},R),l.createElement("span",null,C)),l.createElement("div",{className:"author"},l.createElement(ct,{for:c,substituteIcon:W}),l.createElement("div",{className:"merge-branches"},l.createElement(ht,{for:c})," ",u?null:l.createElement(l.Fragment,null,es(o)," into"," ",l.createElement("code",{className:"branch-tag"},f)," from ",l.createElement("code",{className:"branch-tag"},h)))))}i(Ai,"Subtitle");const So=i(({isCurrentlyCheckedOut:o,isIssue:a,repositoryDefaultBranch:u,owner:c,repo:f,number:h})=>{const{exitReviewMode:y,checkout:C,openChanges:E}=(0,l.useContext)(xe),[R,F]=(0,l.useState)(!1),W=i(async me=>{try{switch(F(!0),me){case"checkout":await C();break;case"exitReviewMode":await y();break;case"openChanges":await E();break;default:throw new Error(`Can't find action ${me}`)}}finally{F(!1)}},"onClick");if(a)return null;const ae={preventDefaultContextMenuItems:!0,owner:c,repo:f,number:h};ae["github:checkoutMenu"]=!0;const Se=[];return o?Se.push({label:`Checkout '${u}'`,value:"",action:i(()=>W("exitReviewMode"),"action")}):Se.push({label:"Checkout",value:"",action:i(()=>W("checkout"),"action")}),Se.push({label:"Open Changes",value:"",action:i(()=>W("openChanges"),"action")}),l.createElement(qn,{optionsContext:i(()=>JSON.stringify(ae),"optionsContext"),defaultAction:Se[0].action,defaultOptionLabel:i(()=>Se[0].label,"defaultOptionLabel"),defaultOptionValue:i(()=>Se[0].value,"defaultOptionValue"),allOptions:i(()=>Se,"allOptions"),optionsTitle:Se[0].label,disabled:R,hasSingleAction:!1,spreadable:!1})},"CheckoutButton");function To(o,a,u){const c=u?mi:ii,f=u?fi:jn;return o===Q.Merged?{text:"Merged",color:"merged",icon:br}:o===Q.Open?a?{text:"Draft",color:"draft",icon:Hl}:{text:"Open",color:"open",icon:f}:{text:"Closed",color:"closed",icon:c}}i(To,"getStatus");function es(o){return o===Q.Merged?"merged changes":"wants to merge changes"}i(es,"getActionText");function pn(o){const{reviewer:a,state:u}=o.reviewState,{reRequestReview:c}=(0,l.useContext)(xe),f=o.event?kt(o.event):void 0;return l.createElement("div",{className:"section-item reviewer"},l.createElement("div",{className:"avatar-with-author"},l.createElement(ct,{for:a}),l.createElement(ht,{for:a})),l.createElement("div",{className:"reviewer-icons"},u!=="REQUESTED"&&($e(a)||a.accountType!==P.Bot)?l.createElement("button",{className:"icon-button",title:"Re-request review",onClick:i(()=>c(o.reviewState.reviewer.id),"onClick")},li,"\uFE0F"):null,Ii[u],f?l.createElement("div",{role:"alert","aria-label":f}):null))}i(pn,"Reviewer");const Ii={REQUESTED:(0,l.cloneElement)(Ot,{className:"section-icon requested",title:"Awaiting requested review"}),COMMENTED:(0,l.cloneElement)(Al,{className:"section-icon commented",Root:"div",title:"Left review comments"}),APPROVED:(0,l.cloneElement)(Bn,{className:"section-icon approved",title:"Approved these changes"}),CHANGES_REQUESTED:(0,l.cloneElement)(ro,{className:"section-icon changes",title:"Requested changes"})},Mo=i(({busy:o,baseHasMergeQueue:a})=>o?l.createElement("label",{htmlFor:"automerge-checkbox",className:"automerge-checkbox-label"},"Setting..."):l.createElement("label",{htmlFor:"automerge-checkbox",className:"automerge-checkbox-label"},a?"Merge when ready":"Auto-merge"),"AutoMergeLabel"),Zt=i(({updateState:o,baseHasMergeQueue:a,allowAutoMerge:u,defaultMergeMethod:c,mergeMethodsAvailability:f,autoMerge:h,isDraft:y})=>{if(!u&&!h||!f||!c)return null;const C=l.useRef(),[E,R]=l.useState(!1),F=i(()=>{var W,ae;return(ae=(W=C.current)==null?void 0:W.value)!=null?ae:"merge"},"selectedMethod");return l.createElement("div",{className:"automerge-section"},l.createElement("div",{className:"automerge-checkbox-wrapper"},l.createElement("input",{id:"automerge-checkbox",type:"checkbox",name:"automerge",checked:h,disabled:!u||y||E,onChange:i(async()=>{R(!0),await o({autoMerge:!h,autoMergeMethod:F()}),R(!1)},"onChange")})),l.createElement(Mo,{busy:E,baseHasMergeQueue:a}),a?null:l.createElement("div",{className:"merge-select-container"},l.createElement(ji,{ref:C,defaultMergeMethod:c,mergeMethodsAvailability:f,onChange:i(async()=>{R(!0),await o({autoMergeMethod:F()}),R(!1)},"onChange"),disabled:E})))},"AutoMerge"),Pr=i(({mergeQueueEntry:o})=>{const a=l.useContext(xe);let u,c;switch(o.state){case w.Mergeable:case w.AwaitingChecks:case w.Queued:{c=l.createElement("span",{className:"merge-queue-pending"},"Queued to merge..."),o.position===1?u=l.createElement("span",null,"This pull request is at the head of the ",l.createElement("a",{href:o.url},"merge queue"),"."):u=l.createElement("span",null,"This pull request is in the ",l.createElement("a",{href:o.url},"merge queue"),".");break}case w.Locked:{c=l.createElement("span",{className:"merge-queue-blocked"},"Merging is blocked"),u=l.createElement("span",null,"The base branch does not allow updates");break}case w.Unmergeable:{c=l.createElement("span",{className:"merge-queue-blocked"},"Merging is blocked"),u=l.createElement("span",null,"There are conflicts with the base branch.");break}}return l.createElement("div",{className:"merge-queue-container"},l.createElement("div",{className:"merge-queue"},l.createElement("div",{className:"merge-queue-icon"}),l.createElement("div",{className:"merge-queue-title"},c),u),l.createElement("div",{className:"button-container"},l.createElement("button",{onClick:a.dequeue},"Remove from Queue")))},"QueuedToMerge");var qt,nr=new Uint8Array(16);function rr(){if(!qt&&(qt=typeof crypto!="undefined"&&crypto.getRandomValues&&crypto.getRandomValues.bind(crypto)||typeof msCrypto!="undefined"&&typeof msCrypto.getRandomValues=="function"&&msCrypto.getRandomValues.bind(msCrypto),!qt))throw new Error("crypto.getRandomValues() not supported. See https://github.com/uuidjs/uuid#getrandomvalues-not-supported");return qt(nr)}i(rr,"rng");const hn=/^(?:[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}|00000000-0000-0000-0000-000000000000)$/i;function No(o){return typeof o=="string"&&hn.test(o)}i(No,"validate");const $t=No;for(var ot=[],Rr=0;Rr<256;++Rr)ot.push((Rr+256).toString(16).substr(1));function ts(o){var a=arguments.length>1&&arguments[1]!==void 0?arguments[1]:0,u=(ot[o[a+0]]+ot[o[a+1]]+ot[o[a+2]]+ot[o[a+3]]+"-"+ot[o[a+4]]+ot[o[a+5]]+"-"+ot[o[a+6]]+ot[o[a+7]]+"-"+ot[o[a+8]]+ot[o[a+9]]+"-"+ot[o[a+10]]+ot[o[a+11]]+ot[o[a+12]]+ot[o[a+13]]+ot[o[a+14]]+ot[o[a+15]]).toLowerCase();if(!$t(u))throw TypeError("Stringified UUID is invalid");return u}i(ts,"stringify");const Po=ts;function ns(o,a,u){o=o||{};var c=o.random||(o.rng||rr)();if(c[6]=c[6]&15|64,c[8]=c[8]&63|128,a){u=u||0;for(var f=0;f<16;++f)a[u+f]=c[f];return a}return Po(c)}i(ns,"v4");const Sn=ns;var Qt=(o=>(o[o.esc=27]="esc",o[o.down=40]="down",o[o.up=38]="up",o))(Qt||{});const Ro=i(({options:o,defaultOption:a,disabled:u,submitAction:c,changeAction:f})=>{const[h,y]=(0,l.useState)(a),[C,E]=(0,l.useState)(!1),R=Sn(),F=`expandOptions${R}`,W=i(()=>{E(!C)},"onClick"),ae=i(Pe=>{y(Pe.target.value),E(!1);const we=document.getElementById(`confirm-button${R}`);we==null||we.focus(),f&&f(Pe.target.value)},"onMethodChange"),Se=i(Pe=>{if(C){const we=document.activeElement;switch(Pe.keyCode){case 27:E(!1);const Te=document.getElementById(F);Te==null||Te.focus();break;case 40:if(!(we!=null&&we.id)||we.id===F){const Ze=document.getElementById(`${R}option0`);Ze==null||Ze.focus()}else{const Ze=new RegExp(`${R}option([0-9])`),Be=we.id.match(Ze);if(Be!=null&&Be.length){const Ye=parseInt(Be[1]);if(Ye<Object.entries(o).length-1){const Xe=document.getElementById(`${R}option${Ye+1}`);Xe==null||Xe.focus()}}}break;case 38:if(!(we!=null&&we.id)||we.id===F){const Ze=Object.entries(o).length-1,Be=document.getElementById(`${R}option${Ze}`);Be==null||Be.focus()}else{const Ze=new RegExp(`${R}option([0-9])`),Be=we.id.match(Ze);if(Be!=null&&Be.length){const Ye=parseInt(Be[1]);if(Ye>0){const Xe=document.getElementById(`${R}option${Ye-1}`);Xe==null||Xe.focus()}}}break}}},"onKeyDown"),me=Object.entries(o).length===1?"hidden":C?"open":"";return l.createElement("div",{className:"select-container",onKeyDown:Se},l.createElement("div",{className:"select-control"},l.createElement(Or,{dropdownId:R,className:Object.keys(o).length>1?"select-left":"",options:o,selected:h,submitAction:c,disabled:!!u}),l.createElement("div",{className:`split${u?" disabled":""}`},l.createElement("div",{className:`separator${u?" disabled":""}`})),l.createElement("button",{id:F,className:"select-right "+me,"aria-label":"Expand button options",onClick:W},ni)),l.createElement("div",{className:C?"options-select":"hidden"},Object.entries(o).map(([Pe,we],Te)=>l.createElement("button",{id:`${R}option${Te}`,key:Pe,value:Pe,onClick:ae},we))))},"Dropdown");function Or({dropdownId:o,className:a,options:u,selected:c,disabled:f,submitAction:h}){const[y,C]=(0,l.useState)(!1),E=i(async R=>{R.preventDefault();try{C(!0),await h(c)}finally{C(!1)}},"onSubmit");return l.createElement("form",{onSubmit:E},l.createElement("input",{disabled:y||f,type:"submit",className:a,id:`confirm-button${o}`,value:u[c]}))}i(Or,"Confirm");const Hi=i(({pr:o,isSimple:a})=>o.state===Q.Merged?l.createElement("div",{className:"branch-status-message"},l.createElement("div",{className:"branch-status-icon"},a?br:null)," ","Pull request successfully merged."):o.state===Q.Closed?l.createElement("div",{className:"branch-status-message"},"This pull request is closed."):null,"PRStatusMessage"),Dr=i(({pr:o})=>o.state===Q.Open?null:l.createElement(zt,{...o}),"DeleteOption"),Ar=i(({pr:o})=>{var a;const{state:u,status:c}=o,[f,h]=(0,l.useReducer)(y=>!y,(a=c==null?void 0:c.statuses.some(y=>y.state===ge.Failure))!=null?a:!1);return(0,l.useEffect)(()=>{var y;(y=c==null?void 0:c.statuses.some(C=>C.state===ge.Failure))!=null&&y?f||h():f&&h()},c==null?void 0:c.statuses),u===Q.Open&&(c!=null&&c.statuses.length)?l.createElement(l.Fragment,null,l.createElement("div",{className:"status-section"},l.createElement("div",{className:"status-item"},l.createElement(Ui,{state:c.state}),l.createElement("p",{className:"status-item-detail-text"},cs(c.statuses)),l.createElement("button",{id:"status-checks-display-button",className:"secondary small-button",onClick:h,"aria-expanded":f},f?"Hide":"Show")),f?l.createElement(us,{statuses:c.statuses}):null)):null},"StatusChecks"),Et=i(({pr:o})=>{const{state:a,reviewRequirement:u}=o;return!u||a!==Q.Open?null:l.createElement(l.Fragment,null,l.createElement("div",{className:"status-section"},l.createElement("div",{className:"status-item"},l.createElement(Wi,{state:u.state}),l.createElement("p",{className:"status-item-detail-text"},Zi(u)))))},"RequiredReviewers"),rs=i(({pr:o,isSimple:a})=>{if(!a||o.state!==Q.Open||o.reviewers.length===0)return null;const u=[],c=new Set(o.reviewers);let f=o.events.length-1;for(;f>=0&&c.size>0;){const h=o.events[f];if(h.event===q.Reviewed){for(const y of c)if(h.user.id===y.reviewer.id){u.push({event:h,reviewState:y}),c.delete(y);break}}f--}return l.createElement("div",{className:"section"}," ",u.map(h=>l.createElement(pn,{key:ke(h.reviewState.reviewer),...h})))},"InlineReviewers"),os=i(({pr:o,isSimple:a})=>o.isIssue?null:l.createElement("div",{id:"status-checks"},l.createElement(l.Fragment,null,l.createElement(Hi,{pr:o,isSimple:a}),l.createElement(Et,{pr:o}),l.createElement(Ar,{pr:o}),l.createElement(rs,{pr:o,isSimple:a}),l.createElement(Fi,{pr:o,isSimple:a}),l.createElement(Dr,{pr:o}))),"StatusChecksSection"),Fi=i(({pr:o,isSimple:a})=>{const{create:u,checkMergeability:c}=(0,l.useContext)(xe);if(a&&o.state!==Q.Open)return l.createElement("div",{className:"branch-status-container"},l.createElement("form",null,l.createElement("button",{type:"submit",onClick:u},"Create New Pull Request...")));if(o.state!==Q.Open)return null;const{mergeable:f}=o,[h,y]=(0,l.useState)(f);return f!==h&&f!==ue.Unknown&&y(f),(0,l.useEffect)(()=>{const C=setInterval(async()=>{if(h===ue.Unknown){const E=await c();y(E)}},3e3);return()=>clearInterval(C)},[h]),l.createElement("div",null,l.createElement(is,{mergeable:h,isSimple:a,canUpdateBranch:o.canUpdateBranch}),l.createElement(ls,{mergeable:h,isSimple:a,isCurrentlyCheckedOut:o.isCurrentlyCheckedOut,canUpdateBranch:o.canUpdateBranch}),l.createElement(ss,{pr:{...o,mergeable:h},isSimple:a}))},"MergeStatusAndActions"),ba=null,is=i(({mergeable:o,isSimple:a,canUpdateBranch:u})=>{const{updateBranch:c}=(0,l.useContext)(xe),[f,h]=(0,l.useState)(!1),y=i(()=>{h(!0),c().finally(()=>h(!1))},"onClick");let C=Ot,E="Checking if this branch can be merged...",R=null;return o===ue.Mergeable?(C=Bn,E="This branch has no conflicts with the base branch."):o===ue.Conflict?(C=_t,E="This branch has conflicts that must be resolved.",R="Resolve conflicts"):o===ue.NotMergeable?(C=_t,E="Branch protection policy must be fulfilled before merging."):o===ue.Behind&&(C=_t,E="This branch is out-of-date with the base branch.",R="Update with merge commit"),a&&(C=null,o!==ue.Conflict&&(R=null)),l.createElement("div",{className:"status-item status-section"},C,l.createElement("p",null,E),R&&u?l.createElement("div",{className:"button-container"},l.createElement("button",{className:"secondary",onClick:y,disabled:f},R)):null)},"MergeStatus"),ls=i(({mergeable:o,isSimple:a,isCurrentlyCheckedOut:u,canUpdateBranch:c})=>{const{updateBranch:f}=(0,l.useContext)(xe),[h,y]=(0,l.useState)(!1),C=i(()=>{y(!0),f().finally(()=>y(!1))},"update"),E=!u&&o===ue.Conflict;return!c||E||a||o===ue.Behind||o===ue.Conflict||o===ue.Unknown?null:l.createElement("div",{className:"status-item status-section"},Wt,l.createElement("p",null,"This branch is out-of-date with the base branch."),l.createElement("button",{className:"secondary",onClick:C,disabled:h},"Update with Merge Commit"))},"OfferToUpdate"),Oo=i(({isSimple:o})=>{const[a,u]=(0,l.useState)(!1),{readyForReview:c,updatePR:f}=(0,l.useContext)(xe),h=(0,l.useCallback)(async()=>{try{u(!0);const y=await c();f(y)}finally{u(!1)}},[u,c,f]);return l.createElement("div",{className:"ready-for-review-container"},l.createElement("div",{className:"ready-for-review-text-wrapper"},l.createElement("div",{className:"ready-for-review-icon"},o?null:Wt),l.createElement("div",null,l.createElement("div",{className:"ready-for-review-heading"},"This pull request is still a work in progress."),l.createElement("div",{className:"ready-for-review-meta"},"Draft pull requests cannot be merged."))),l.createElement("div",{className:"button-container"},l.createElement("button",{disabled:a,onClick:h},"Ready for Review")))},"ReadyForReview"),or=i(o=>{const a=(0,l.useContext)(xe),u=(0,l.useRef)(),[c,f]=(0,l.useState)(null);return o.mergeQueueMethod?l.createElement("div",null,l.createElement("div",{id:"merge-comment-form"},l.createElement("button",{onClick:i(()=>a.enqueue(),"onClick")},"Add to Merge Queue"))):c?l.createElement($i,{pr:o,method:c,cancel:i(()=>f(null),"cancel")}):l.createElement("div",{className:"automerge-section wrapper"},l.createElement("button",{onClick:i(()=>f(u.current.value),"onClick")},"Merge Pull Request"),rt,"using method",rt,l.createElement(ji,{ref:u,...o}))},"Merge"),ss=i(({pr:o,isSimple:a})=>{var u;const{hasWritePermission:c,canEdit:f,isDraft:h,mergeable:y}=o;if(h)return f?l.createElement(Oo,{isSimple:a}):null;if(y===ue.Mergeable&&c&&!o.mergeQueueEntry)return a?l.createElement(Vi,{...o}):l.createElement(or,{...o});if(!a&&c&&!o.mergeQueueEntry){const C=(0,l.useContext)(xe);return l.createElement(Zt,{updateState:i(E=>C.updateAutoMerge(E),"updateState"),...o,baseHasMergeQueue:!!o.mergeQueueMethod,defaultMergeMethod:(u=o.autoMergeMethod)!=null?u:o.defaultMergeMethod})}else if(o.mergeQueueEntry)return l.createElement(Pr,{mergeQueueEntry:o.mergeQueueEntry});return null},"PrActions"),as=i(()=>{const{openOnGitHub:o}=useContext(PullRequestContext);return React.createElement("button",{id:"merge-on-github",type:"submit",onClick:i(()=>o(),"onClick")},"Merge on github.com")},"MergeOnGitHub"),Vi=i(o=>{const{merge:a,updatePR:u}=(0,l.useContext)(xe);async function c(h){const y=await a({title:"",description:"",method:h});u(y)}i(c,"submitAction");const f=Object.keys(Bt).filter(h=>o.mergeMethodsAvailability[h]).reduce((h,y)=>(h[y]=Bt[y],h),{});return l.createElement(Ro,{options:f,defaultOption:o.defaultMergeMethod,submitAction:c})},"MergeSimple"),zt=i(o=>{const{deleteBranch:a}=(0,l.useContext)(xe),[u,c]=(0,l.useState)(!1);return o.isRemoteHeadDeleted!==!1&&o.isLocalHeadDeleted!==!1?l.createElement("div",null):l.createElement("div",{className:"branch-status-container"},l.createElement("form",{onSubmit:i(async f=>{f.preventDefault();try{c(!0);const h=await a();h&&h.cancelled&&c(!1)}finally{c(!1)}},"onSubmit")},l.createElement("button",{disabled:u,className:"secondary",type:"submit"},"Delete Branch...")))},"DeleteBranch");function $i({pr:o,method:a,cancel:u}){const{merge:c,updatePR:f,changeEmail:h}=(0,l.useContext)(xe),[y,C]=(0,l.useState)(!1),E=o.emailForCommit;return l.createElement("div",null,l.createElement("form",{id:"merge-comment-form",onSubmit:i(async R=>{R.preventDefault();try{C(!0);const{title:F,description:W}=R.target,ae=await c({title:F==null?void 0:F.value,description:W==null?void 0:W.value,method:a,email:E});f(ae)}finally{C(!1)}},"onSubmit")},a==="rebase"?null:l.createElement("input",{type:"text",name:"title",defaultValue:zi(a,o)}),a==="rebase"?null:l.createElement("textarea",{name:"description",defaultValue:Bi(a,o)}),a==="rebase"||!E?null:l.createElement("div",{className:"commit-association"},l.createElement("span",null,"Commit will be associated with ",l.createElement("button",{className:"input-box",title:"Change email","aria-label":"Change email",disabled:y,onClick:i(()=>{C(!0),h(E).finally(()=>C(!1))},"onClick")},E))),l.createElement("div",{className:"form-actions",id:a==="rebase"?"rebase-actions":""},l.createElement("button",{className:"secondary",onClick:u},"Cancel"),l.createElement("button",{disabled:y,type:"submit",id:"confirm-merge"},a==="rebase"?"Confirm ":"",Bt[a]))))}i($i,"ConfirmMerge");function zi(o,a){var u,c,f,h;switch(o){case"merge":return(c=(u=a.mergeCommitMeta)==null?void 0:u.title)!=null?c:`Merge pull request #${a.number} from ${a.head}`;case"squash":return(h=(f=a.squashCommitMeta)==null?void 0:f.title)!=null?h:`${a.title} (#${a.number})`;default:return""}}i(zi,"getDefaultTitleText");function Bi(o,a){var u,c,f,h;switch(o){case"merge":return(c=(u=a.mergeCommitMeta)==null?void 0:u.description)!=null?c:a.title;case"squash":return(h=(f=a.squashCommitMeta)==null?void 0:f.description)!=null?h:"";default:return""}}i(Bi,"getDefaultDescriptionText");const Bt={merge:"Create Merge Commit",squash:"Squash and Merge",rebase:"Rebase and Merge"},ji=l.forwardRef(({defaultMergeMethod:o,mergeMethodsAvailability:a,onChange:u,ariaLabel:c,name:f,title:h,disabled:y},C)=>l.createElement("select",{ref:C,defaultValue:o,onChange:u,disabled:y,"aria-label":c!=null?c:"Select merge method",name:f,title:h},Object.entries(Bt).map(([E,R])=>l.createElement("option",{key:E,value:E,disabled:!a[E]},R,a[E]?null:" (not enabled)")))),us=i(({statuses:o})=>l.createElement("div",{className:"status-scroll"},o.map(a=>l.createElement("div",{key:a.id,className:"status-check"},l.createElement("div",{className:"status-check-details"},l.createElement(Ui,{state:a.state}),l.createElement(ct,{for:{avatarUrl:a.avatarUrl,url:a.url}}),l.createElement("span",{className:"status-check-detail-text"},a.workflowName?`${a.workflowName} / `:null,a.context,a.event?` (${a.event})`:null," ",a.description?`\u2014 ${a.description}`:null)),l.createElement("div",null,a.isRequired?l.createElement("span",{className:"label"},"Required"):null,a.targetUrl?l.createElement("a",{href:a.targetUrl,title:a.targetUrl},"Details"):null)))),"StatusCheckDetails");function cs(o){const a=ao(o,c=>{switch(c.state){case ge.Success:case ge.Failure:case ge.Neutral:return c.state;default:return ge.Pending}}),u=[];for(const c of Object.keys(a)){const f=a[c].length;let h="";switch(c){case ge.Success:h="successful";break;case ge.Failure:h="failed";break;case ge.Neutral:h="skipped";break;default:h="pending"}const y=f>1?`${f} ${h} checks`:`${f} ${h} check`;u.push(y)}return u.join(" and ")}i(cs,"getSummaryLabel");function Ui({state:o}){switch(o){case ge.Neutral:return Er;case ge.Success:return Bn;case ge.Failure:return _t}return Ot}i(Ui,"StateIcon");function Wi({state:o}){switch(o){case ge.Pending:return ro;case ge.Failure:return _t}return Bn}i(Wi,"RequiredReviewStateIcon");function Zi(o){const a=o.approvals.length,u=o.requestedChanges.length,c=o.count;switch(o.state){case ge.Failure:return`At least ${c} approving review${c>1?"s":""} is required by reviewers with write access.`;case ge.Pending:return`${u} review${u>1?"s":""} requesting changes by reviewers with write access.`}return`${a} approving review${a>1?"s":""} by reviewers with write access.`}i(Zi,"getRequiredReviewSummary");function qi(o){const{displayName:a,canDelete:u,color:c}=o,f=un(c,o.isDarkTheme,!1);return l.createElement("div",{className:"section-item label",style:{backgroundColor:f.backgroundColor,color:f.textColor,borderColor:`${f.borderColor}`,paddingRight:u?"2px":"8px"}},a,o.children)}i(qi,"Label");function Ir(o){const{displayName:a,color:u}=o,c=gitHubLabelColor(u,o.isDarkTheme,!1);return React.createElement("li",{style:{backgroundColor:c.backgroundColor,color:c.textColor,borderColor:`${c.borderColor}`}},a,o.children)}i(Ir,"LabelCreate");function Tn({reviewers:o,labels:a,hasWritePermission:u,isIssue:c,projectItems:f,milestone:h,assignees:y,canAssignCopilot:C}){const{addReviewers:E,addAssignees:R,addAssigneeYourself:F,addAssigneeCopilot:W,addLabels:ae,removeLabel:Se,changeProjects:me,addMilestone:Pe,updatePR:we,pr:Te}=(0,l.useContext)(xe),[Ze,Be]=(0,l.useState)(!1),Ye=C&&y.every(_e=>!Oi.includes(_e.login)),Xe=i(async()=>{const _e=await me();we({..._e})},"updateProjects");return l.createElement("div",{id:"sidebar"},c?"":l.createElement("div",{id:"reviewers",className:"section"},l.createElement("div",{className:"section-header",onClick:i(async()=>{const _e=await E();we({reviewers:_e.reviewers})},"onClick")},l.createElement("div",{className:"section-title"},"Reviewers"),u?l.createElement("button",{className:"icon-button",title:"Add Reviewers"},Un):null),o&&o.length?o.map(_e=>l.createElement(pn,{key:ke(_e.reviewer),reviewState:_e})):l.createElement("div",{className:"section-placeholder"},"None yet")),l.createElement("div",{id:"assignees",className:"section"},l.createElement("div",{className:"section-header",onClick:i(async _e=>{if(_e.target.closest("#assign-copilot-btn"))return;const At=await R();we({assignees:At.assignees,events:At.events})},"onClick")},l.createElement("div",{className:"section-title"},"Assignees"),u?l.createElement("div",{className:"icon-button-group"},Ye?l.createElement("button",{id:"assign-copilot-btn",className:"icon-button",title:"Assign for Copilot to work on",disabled:Ze,onClick:i(async()=>{Be(!0);try{const _e=await W();we({assignees:_e.assignees,events:_e.events})}finally{Be(!1)}},"onClick")},pi):null,l.createElement("button",{className:"icon-button",title:"Add Assignees"},Un)):null),y&&y.length?y.map((_e,lt)=>l.createElement("div",{key:lt,className:"section-item reviewer"},l.createElement("div",{className:"avatar-with-author"},l.createElement(ct,{for:_e}),l.createElement(ht,{for:_e})))):l.createElement("div",{className:"section-placeholder"},"None yet",Te.hasWritePermission?l.createElement(l.Fragment,null,"\u2014",l.createElement("a",{className:"assign-yourself",onClick:i(async()=>{const _e=await F();we({assignees:_e.assignees,events:_e.events})},"onClick")},"assign yourself")):null)),l.createElement("div",{id:"labels",className:"section"},l.createElement("div",{className:"section-header",onClick:i(async()=>{const _e=await ae();we({labels:_e.added})},"onClick")},l.createElement("div",{className:"section-title"},"Labels"),u?l.createElement("button",{className:"icon-button",title:"Add Labels"},Un):null),a.length?l.createElement("div",{className:"labels-list"},a.map(_e=>l.createElement(qi,{key:_e.name,..._e,canDelete:u,isDarkTheme:Te.isDarkTheme},u?l.createElement("button",{className:"icon-button",onClick:i(()=>Se(_e.name),"onClick")},_t,"\uFE0F"):null))):l.createElement("div",{className:"section-placeholder"},"None yet")),Te.isEnterprise?null:l.createElement("div",{id:"project",className:"section"},l.createElement("div",{className:"section-header",onClick:Xe},l.createElement("div",{className:"section-title"},"Project"),u?l.createElement("button",{className:"icon-button",title:"Add Project"},Un):null),f?f.length>0?f.map(_e=>l.createElement(Hr,{key:_e.project.title,..._e,canDelete:u})):l.createElement("div",{className:"section-placeholder"},"None yet"):l.createElement("a",{onClick:Xe},"Sign in with more permissions to see projects")),l.createElement("div",{id:"milestone",className:"section"},l.createElement("div",{className:"section-header",onClick:i(async()=>{const _e=await Pe();we({milestone:_e.added})},"onClick")},l.createElement("div",{className:"section-title"},"Milestone"),u?l.createElement("button",{className:"icon-button",title:"Add Milestone"},Un):null),h?l.createElement(ds,{key:h.title,...h,canDelete:u}):l.createElement("div",{className:"section-placeholder"},"No milestone")))}i(Tn,"Sidebar");function ds(o){const{removeMilestone:a,updatePR:u,pr:c}=(0,l.useContext)(xe),f=getComputedStyle(document.documentElement).getPropertyValue("--vscode-badge-foreground"),h=un(f,c.isDarkTheme,!1),{canDelete:y,title:C}=o;return l.createElement("div",{className:"labels-list"},l.createElement("div",{className:"section-item label",style:{backgroundColor:h.backgroundColor,color:h.textColor,borderColor:`${h.borderColor}`}},C,y?l.createElement("button",{className:"icon-button",onClick:i(async()=>{await a(),u({milestone:void 0})},"onClick")},_t,"\uFE0F"):null))}i(ds,"Milestone");function Hr(o){const{removeProject:a,updatePR:u,pr:c}=(0,l.useContext)(xe),f=getComputedStyle(document.documentElement).getPropertyValue("--vscode-badge-foreground"),h=un(f,c.isDarkTheme,!1),{canDelete:y}=o;return l.createElement("div",{className:"labels-list"},l.createElement("div",{className:"section-item label",style:{backgroundColor:h.backgroundColor,color:h.textColor,borderColor:`${h.borderColor}`}},o.project.title,y?l.createElement("button",{className:"icon-button",onClick:i(async()=>{var C;await a(o),u({projectItems:(C=c.projectItems)==null?void 0:C.filter(E=>E.id!==o.id)})},"onClick")},_t,"\uFE0F"):null))}i(Hr,"Project");var fs=(o=>(o[o.ADD=0]="ADD",o[o.COPY=1]="COPY",o[o.DELETE=2]="DELETE",o[o.MODIFY=3]="MODIFY",o[o.RENAME=4]="RENAME",o[o.TYPE=5]="TYPE",o[o.UNKNOWN=6]="UNKNOWN",o[o.UNMERGED=7]="UNMERGED",o))(fs||{});const $o=class $o{constructor(a,u,c,f,h,y,C){this.baseCommit=a,this.status=u,this.fileName=c,this.previousFileName=f,this.patch=h,this.diffHunks=y,this.blobUrl=C}};i($o,"file_InMemFileChange");let Fr=$o;const cr=class cr{constructor(a,u,c,f,h){this.baseCommit=a,this.blobUrl=u,this.status=c,this.fileName=f,this.previousFileName=h}};i(cr,"file_SlimFileChange");let Do=cr;var Qi=Object.defineProperty,ms=i((o,a,u)=>a in o?Qi(o,a,{enumerable:!0,configurable:!0,writable:!0,value:u}):o[a]=u,"diffHunk_defNormalProp"),ps=i((o,a,u)=>ms(o,typeof a!="symbol"?a+"":a,u),"diffHunk_publicField"),Ki=(o=>(o[o.Context=0]="Context",o[o.Add=1]="Add",o[o.Delete=2]="Delete",o[o.Control=3]="Control",o))(Ki||{});const jr=class jr{constructor(a,u,c,f,h,y=!0){this.type=a,this.oldLineNumber=u,this.newLineNumber=c,this.positionInHunk=f,this._raw=h,this.endwithLineBreak=y}get raw(){return this._raw}get text(){return this._raw.substr(1)}};i(jr,"DiffLine");let Vr=jr;function hs(o){switch(o[0]){case" ":return 0;case"+":return 1;case"-":return 2;default:return 3}}i(hs,"getDiffChangeType");const Ur=class Ur{constructor(a,u,c,f,h){this.oldLineNumber=a,this.oldLength=u,this.newLineNumber=c,this.newLength=f,this.positionInHunk=h,ps(this,"diffLines",[])}};i(Ur,"DiffHunk");let vn=Ur;const Yi=/^@@ \-(\d+)(,(\d+))?( \+(\d+)(,(\d+)?)?)? @@/;function vs(o){let a=0,u=0;for(;(u=o.indexOf("\r",u))!==-1;)u++,a++;return a}i(vs,"countCarriageReturns");function*Ao(o){let a=0;for(;a!==-1&&a<o.length;){const u=a;a=o.indexOf(`
`,a);let f=(a!==-1?a:o.length)-u;a!==-1&&(a>0&&o[a-1]==="\r"&&f--,a++),yield o.substr(u,f)}}i(Ao,"LineReader");function*Io(o){const a=Ao(o);let u=a.next(),c,f=-1,h=-1,y=-1;for(;!u.done;){const C=u.value;if(Yi.test(C)){c&&(yield c,c=void 0),f===-1&&(f=0);const E=Yi.exec(C),R=h=Number(E[1]),F=Number(E[3])||1,W=y=Number(E[5]),ae=Number(E[7])||1;c=new vn(R,F,W,ae,f),c.diffLines.push(new Vr(3,-1,-1,f,C))}else if(c){const E=hs(C);if(E===3)c.diffLines&&c.diffLines.length&&(c.diffLines[c.diffLines.length-1].endwithLineBreak=!1);else{c.diffLines.push(new Vr(E,E!==1?h:-1,E!==2?y:-1,f,C));const R=1+vs(C);switch(E){case 0:h+=R,y+=R;break;case 2:h+=R;break;case 1:y+=R;break}}}f!==-1&&++f,u=a.next()}c&&(yield c)}i(Io,"parseDiffHunk");function Gi(o){const a=Io(o);let u=a.next();const c=[];for(;!u.done;){const f=u.value;c.push(f),u=a.next()}return c}i(Gi,"parsePatch");function gs(o){const a=[],u=i(E=>({diffLines:[],newLength:0,oldLength:0,oldLineNumber:E.oldLineNumber,newLineNumber:E.newLineNumber,positionInHunk:0}),"newHunk");let c,f;const h=i((E,R)=>{E.diffLines.push(R),R.type===2?E.oldLength++:R.type===1?E.newLength++:R.type===0&&(E.oldLength++,E.newLength++)},"addLineToHunk"),y=i(E=>E.diffLines.some(R=>R.type!==0),"hunkHasChanges"),C=i(E=>y(E)&&E.diffLines[E.diffLines.length-1].type===0,"hunkHasSandwichedChanges");for(const E of o.diffLines)E.type===0?(c||(c=u(E)),h(c,E),C(c)&&(f||(f=u(E)),h(f,E))):(c||o.oldLineNumber===1&&(E.type===2||E.type===1))&&(c||(c=u(E)),C(c)&&(a.push(c),c=f,f=void 0),(E.type===2||E.type===1)&&h(c,E));return c&&a.push(c),a}i(gs,"splitIntoSmallerHunks");function ys(o,a){const u=o.split(/\r?\n/),c=Io(a);let f=c.next();const h=[],y=[];let C=0,E=!0;for(;!f.done;){const R=f.value;h.push(R);const F=R.oldLineNumber;for(let W=C+1;W<F;W++)y.push(u[W-1]);C=F+R.oldLength-1;for(let W=0;W<R.diffLines.length;W++){const ae=R.diffLines[W];if(!(ae.type===2||ae.type===3))if(ae.type===1)y.push(ae.text);else{const Se=ae.text;y.push(Se)}}if(f=c.next(),f.done){for(let W=R.diffLines.length-1;W>=0;W--)if(R.diffLines[W].type!==2){E=R.diffLines[W].endwithLineBreak;break}}}if(E)if(C<u.length)for(let R=C+1;R<=u.length;R++)y.push(u[R-1]);else y.push("");return y.join(`
`)}i(ys,"getModifiedContentFromDiffHunk");function ir(o){switch(o){case"removed":return GitChangeType.DELETE;case"added":return GitChangeType.ADD;case"renamed":return GitChangeType.RENAME;case"modified":return GitChangeType.MODIFY;default:return GitChangeType.UNKNOWN}}i(ir,"getGitChangeType");async function Cs(o,a){var u;const c=[];for(let f=0;f<o.length;f++){const h=o[f],y=ir(h.status);if(!h.patch&&y!==GitChangeType.RENAME&&y!==GitChangeType.MODIFY&&!(y===GitChangeType.ADD&&h.additions===0)){c.push(new SlimFileChange(a,h.blob_url,y,h.filename,h.previous_filename));continue}const C=h.patch?Gi(h.patch):void 0;c.push(new InMemFileChange(a,y,h.filename,h.previous_filename,(u=h.patch)!=null?u:"",C,h.blob_url))}return c}i(Cs,"parseDiff");function lr({hunks:o}){return l.createElement("div",{className:"diff"},o.map((a,u)=>l.createElement(xs,{key:u,hunk:a})))}i(lr,"Diff");const ws=lr,xs=i(({hunk:o,maxLines:a=8})=>l.createElement(l.Fragment,null,o.diffLines.slice(-a).map(u=>l.createElement("div",{key:gn(u),className:`diffLine ${sr(u.type)}`},l.createElement(Xi,{num:u.oldLineNumber}),l.createElement(Xi,{num:u.newLineNumber}),l.createElement("div",{className:"diffTypeSign"},u._raw.substr(0,1)),l.createElement("div",{className:"lineContent"},u._raw.substr(1))))),"Hunk"),gn=i(o=>`${o.oldLineNumber}->${o.newLineNumber}`,"keyForDiffLine"),Xi=i(({num:o})=>l.createElement("div",{className:"lineNumber"},o>0?o:" "),"LineNumber"),sr=i(o=>Ki[o].toLowerCase(),"getDiffChangeClass");function Ji(o){return o.event===q.Assigned||o.event===q.Unassigned}i(Ji,"isAssignUnassignEvent");const el=i(({events:o,isIssue:a})=>{var u,c,f,h;const y=[];for(let C=0;C<o.length;C++)if(C>0&&Ji(o[C])&&Ji(y[y.length-1])){const E=y[y.length-1],R=o[C];if(E.actor.login===R.actor.login&&new Date(E.createdAt).getTime()+1e3*60*10>new Date(R.createdAt).getTime()){const F=E.assignees||[],W=E.unassignees||[],ae=(c=(u=R.assignees)==null?void 0:u.filter(me=>!F.some(Pe=>Pe.id===me.id)))!=null?c:[],Se=(h=(f=R.unassignees)==null?void 0:f.filter(me=>!W.some(Pe=>Pe.id===me.id)))!=null?h:[];E.assignees=[...F,...ae],E.unassignees=[...W,...Se]}else y.push(R)}else y.push(o[C]);return l.createElement(l.Fragment,null,y.map(C=>{switch(C.event){case q.Committed:return l.createElement(Ho,{key:`commit${C.id}`,...C});case q.Reviewed:return l.createElement(Es,{key:`review${C.id}`,...C});case q.Commented:return l.createElement(_s,{key:`comment${C.id}`,...C});case q.Merged:return l.createElement($r,{key:`merged${C.id}`,...C});case q.Assigned:return l.createElement(rl,{key:`assign${C.id}`,event:C});case q.Unassigned:return l.createElement(rl,{key:`unassign${C.id}`,event:C});case q.HeadRefDeleted:return l.createElement(Ls,{key:`head${C.id}`,...C});case q.CrossReferenced:return l.createElement(Ss,{key:`cross${C.id}`,...C});case q.Closed:return l.createElement(Ts,{key:`closed${C.id}`,event:C,isIssue:a});case q.Reopened:return l.createElement(Ms,{key:`reopened${C.id}`,event:C,isIssue:a});case q.NewCommitsSinceReview:return l.createElement(ur,{key:`newCommits${C.id}`});case q.CopilotStarted:return l.createElement(Ns,{key:`copilotStarted${C.id}`,...C});case q.CopilotFinished:return l.createElement(Ps,{key:`copilotFinished${C.id}`,...C});case q.CopilotFinishedError:return l.createElement(Rs,{key:`copilotFinishedError${C.id}`,...C});default:throw new Yn(C)}}))},"Timeline"),ar=null,Ho=i(o=>{var a;const u=(0,l.useContext)(xe),[c,f]=(0,l.useState)(void 0),h=i((C,E)=>{C.preventDefault(),f(E),u.openCommitChanges(o.sha).finally(()=>{f(void 0)})},"handleCommitClick"),y=((a=u.pr)==null?void 0:a.loadingCommit)===o.sha;return l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},kr,rt,l.createElement("div",{className:"avatar-container"},l.createElement(ct,{for:o.author})),l.createElement("div",{className:"message-container"},l.createElement("a",{className:"message",onClick:i(C=>h(C,"title"),"onClick"),title:o.htmlUrl},o.message.substr(0,o.message.indexOf(`
`)>-1?o.message.indexOf(`
`):o.message.length)),y&&c==="title"&&l.createElement("span",{className:"commit-spinner-inline"},rn))),l.createElement("div",{className:"timeline-detail"},l.createElement("a",{className:"sha",onClick:i(C=>h(C,"sha"),"onClick"),title:o.htmlUrl},y&&c==="sha"&&l.createElement("span",{className:"commit-spinner-before"},rn),o.sha.slice(0,7)),l.createElement(St,{date:o.committedDate})))},"CommitEventView"),ur=i(()=>{const{gotoChangesSinceReview:o,pr:a}=(0,l.useContext)(xe);if(!a.isCurrentlyCheckedOut)return null;const[u,c]=(0,l.useState)(!1),f=i(async()=>{c(!0),await o(),c(!1)},"viewChanges");return l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},no,rt,l.createElement("span",{style:{fontWeight:"bold"}},"New changes since your last Review")),l.createElement("button",{"aria-live":"polite",title:"View the changes since your last review",onClick:f,disabled:u},"View Changes"))},"NewCommitsSinceReviewEventView"),Fo=i(o=>o.position!==null?`pos:${o.position}`:`ori:${o.originalPosition}`,"positionKey"),tl=i(o=>ao(o,a=>a.path+":"+Fo(a)),"groupCommentsByPath"),Es=i(o=>{const a=tl(o.comments),u=o.state==="PENDING";return l.createElement(fn,{comment:o,allowEmpty:!0},o.comments.length?l.createElement("div",{className:"comment-body review-comment-body"},Object.entries(a).map(([c,f])=>l.createElement(ks,{key:c,thread:f,event:o}))):null,u?l.createElement(bs,null):null)},"ReviewEventView");function ks({thread:o,event:a}){var u;const c=o[0],[f,h]=(0,l.useState)(!c.isResolved),[y,C]=(0,l.useState)(!!c.isResolved),{openDiff:E,toggleResolveComment:R}=(0,l.useContext)(xe),F=a.reviewThread&&(a.reviewThread.canResolve&&!a.reviewThread.isResolved||a.reviewThread.canUnresolve&&a.reviewThread.isResolved),W=i(()=>{if(a.reviewThread){const ae=!y;h(!ae),C(ae),R(a.reviewThread.threadId,o,ae)}},"toggleResolve");return l.createElement("div",{key:a.id,className:"diff-container"},l.createElement("div",{className:"resolved-container"},l.createElement("div",null,c.position===null?l.createElement("span",null,l.createElement("span",null,c.path),l.createElement("span",{className:"outdatedLabel"},"Outdated")):l.createElement("a",{className:"diffPath",onClick:i(()=>E(c),"onClick")},c.path),!y&&!f?l.createElement("span",{className:"unresolvedLabel"},"Unresolved"):null),l.createElement("button",{className:"secondary",onClick:i(()=>h(!f),"onClick")},f?"Hide":"Show")),f?l.createElement("div",null,l.createElement(ws,{hunks:(u=c.diffHunks)!=null?u:[]}),o.map(ae=>l.createElement(fn,{key:ae.id,comment:ae})),F?l.createElement("div",{className:"resolve-comment-row"},l.createElement("button",{className:"secondary comment-resolve",onClick:i(()=>W(),"onClick")},y?"Unresolve Conversation":"Resolve Conversation")):null):null)}i(ks,"CommentThread");function bs(){const{requestChanges:o,approve:a,submit:u,pr:c}=(0,l.useContext)(xe),{isAuthor:f}=c,h=(0,l.useRef)(),[y,C]=(0,l.useState)(!1);async function E(F,W){F.preventDefault();const{value:ae}=h.current;switch(C(!0),W){case Ne.RequestChanges:await o(ae);break;case Ne.Approve:await a(ae);break;default:await u(ae)}C(!1)}i(E,"submitAction");const R=i(F=>{(F.ctrlKey||F.metaKey)&&F.key==="Enter"&&E(F,Ne.Comment)},"onKeyDown");return l.createElement("form",null,l.createElement("textarea",{id:"pending-review",ref:h,placeholder:"Leave a review summary comment",onKeyDown:R}),l.createElement("div",{className:"form-actions"},f?null:l.createElement("button",{id:"request-changes",className:"secondary",disabled:y||c.busy,onClick:i(F=>E(F,Ne.RequestChanges),"onClick")},"Request Changes"),f?null:l.createElement("button",{id:"approve",className:"secondary",disabled:y||c.busy,onClick:i(F=>E(F,Ne.Approve),"onClick")},"Approve"),l.createElement("button",{disabled:y||c.busy,onClick:i(F=>E(F,Ne.Comment),"onClick")},"Submit Review")))}i(bs,"AddReviewSummaryComment");const _s=i(o=>l.createElement(fn,{headerInEditMode:!0,comment:o}),"CommentEventView"),$r=i(o=>{const{revert:a,pr:u}=(0,l.useContext)(xe);return l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},br,rt,l.createElement("div",{className:"avatar-container"},l.createElement(ct,{for:o.user})),l.createElement(ht,{for:o.user}),l.createElement("div",{className:"message"},"merged commit",rt,l.createElement("a",{className:"sha",href:o.commitUrl,title:o.commitUrl},o.sha.substr(0,7)),rt,"into ",o.mergeRef,rt)),u.revertable?l.createElement("div",{className:"timeline-detail"},l.createElement("button",{className:"secondary",disabled:u.busy,onClick:a},"Revert")):null,l.createElement(St,{href:o.url,date:o.createdAt}))},"MergedEventView"),Ls=i(o=>l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},l.createElement("div",{className:"avatar-container"},l.createElement(ct,{for:o.actor})),l.createElement(ht,{for:o.actor}),l.createElement("div",{className:"message"},"deleted the ",o.headRef," branch",rt)),l.createElement(St,{date:o.createdAt})),"HeadDeleteEventView"),Ss=i(o=>{const{source:a}=o;return l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},l.createElement("div",{className:"avatar-container"},l.createElement(ct,{for:o.actor})),l.createElement(ht,{for:o.actor}),l.createElement("div",{className:"message"},"linked ",l.createElement("a",{href:a.extensionUrl},"#",a.number)," ",a.title,rt,o.willCloseTarget?"which will close this issue":"")),l.createElement(St,{date:o.createdAt}))},"CrossReferencedEventView");function nl(o){return o.length===0?l.createElement(l.Fragment,null):o.length===1?o[0]:o.length===2?l.createElement(l.Fragment,null,o[0]," and ",o[1]):l.createElement(l.Fragment,null,o.slice(0,-1).map(a=>l.createElement(l.Fragment,null,a,", "))," and ",o[o.length-1])}i(nl,"timeline_joinWithAnd");const rl=i(({event:o})=>{const{actor:a}=o,u=o.assignees||[],c=o.unassignees||[],f=nl(u.map(C=>l.createElement(ht,{key:C.id,for:C}))),h=nl(c.map(C=>l.createElement(ht,{key:C.id,for:C})));let y;return u.length>0&&c.length>0?y=l.createElement(l.Fragment,null,"assigned ",f," and unassigned ",h):u.length>0?y=l.createElement(l.Fragment,null,"assigned ",f):y=l.createElement(l.Fragment,null,"unassigned ",h),l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},l.createElement("div",{className:"avatar-container"},l.createElement(ct,{for:a})),l.createElement(ht,{for:a}),l.createElement("div",{className:"message"},y)),l.createElement(St,{date:o.createdAt}))},"AssignUnassignEventView"),Ts=i(({event:o,isIssue:a})=>{const{actor:u,createdAt:c}=o;return l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},l.createElement("div",{className:"avatar-container"},l.createElement(ct,{for:u})),l.createElement(ht,{for:u}),l.createElement("div",{className:"message"},a?"closed this issue":"closed this pull request")),l.createElement(St,{date:c}))},"ClosedEventView"),Ms=i(({event:o,isIssue:a})=>{const{actor:u,createdAt:c}=o;return l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},l.createElement("div",{className:"avatar-container"},l.createElement(ct,{for:u})),l.createElement(ht,{for:u}),l.createElement("div",{className:"message"},a?"reopened this issue":"reopened this pull request")),l.createElement(St,{date:c}))},"ReopenedEventView"),Ns=i(o=>{const{createdAt:a,onBehalfOf:u,sessionLink:c}=o,{openSessionLog:f}=(0,l.useContext)(xe),h=i(y=>{c&&(c.openToTheSide=y.ctrlKey||y.metaKey,f(c))},"handleSessionLogClick");return l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},hi,rt,l.createElement("div",{className:"message"},"Copilot started work on behalf of ",l.createElement(ht,{for:u}))),c?l.createElement("div",{className:"timeline-detail"},l.createElement("a",{onClick:h},l.createElement("button",{className:"secondary",title:"View session log (Ctrl/Cmd+Click to open in second editor group)"},"View session"))):null,l.createElement(St,{date:a}))},"CopilotStartedEventView"),Ps=i(o=>{const{createdAt:a,onBehalfOf:u}=o;return l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"commit-message"},vi,rt,l.createElement("div",{className:"message"},"Copilot finished work on behalf of ",l.createElement(ht,{for:u}))),l.createElement(St,{date:a}))},"CopilotFinishedEventView"),Rs=i(o=>{const{createdAt:a,onBehalfOf:u}=o,{openSessionLog:c}=(0,l.useContext)(xe),f=i(h=>{o.sessionLink.openToTheSide=h.ctrlKey||h.metaKey,c(o.sessionLink)},"handleSessionLogClick");return l.createElement("div",{className:"comment-container commit"},l.createElement("div",{className:"timeline-with-detail"},l.createElement("div",{className:"commit-message"},oo,rt,l.createElement("div",{className:"message"},"Copilot stopped work on behalf of ",l.createElement(ht,{for:u})," due to an error")),l.createElement("div",{className:"commit-message-detail"},l.createElement("a",{onClick:f,title:"View session log (Ctrl/Cmd+Click to open in second editor group)"},"Copilot has encountered an error. See logs for additional details."))),l.createElement(St,{date:a}))},"CopilotFinishedErrorEventView"),Vo=i(o=>{const[a,u]=l.useState(window.matchMedia(o).matches);return l.useEffect(()=>{const c=window.matchMedia(o),f=i(()=>u(c.matches),"documentChangeHandler");return c.addEventListener("change",f),()=>{c.removeEventListener("change",f)}},[o]),a},"useMediaQuery"),Mn=i(o=>{const a=Vo("(max-width: 925px)");return l.createElement(l.Fragment,null,l.createElement("div",{id:"title",className:"title"},l.createElement("div",{className:"details"},l.createElement(ko,{...o}))),a?l.createElement(l.Fragment,null,l.createElement(Tn,{...o}),l.createElement(We,{...o})):l.createElement(l.Fragment,null,l.createElement(We,{...o}),l.createElement(Tn,{...o})))},"Overview"),We=i(o=>l.createElement("div",{id:"main"},l.createElement("div",{id:"description"},l.createElement(fn,{isPRDescription:!0,comment:o})),l.createElement(el,{events:o.events,isIssue:o.isIssue}),l.createElement(os,{pr:o,isSimple:!1}),l.createElement(Ri,{...o})),"Main");function Ge(){(0,oe.render)(l.createElement(Kt,null,o=>l.createElement(Mn,{...o})),document.getElementById("app"))}i(Ge,"main");function Kt({children:o}){const a=(0,l.useContext)(xe),[u,c]=(0,l.useState)(a.pr);return(0,l.useEffect)(()=>{a.onchange=c,c(a.pr)},[]),window.onscroll=N(()=>{a.postMessage({command:"scroll",args:{scrollPosition:{x:window.scrollX,y:window.scrollY}}})},200),a.postMessage({command:"ready"}),a.postMessage({command:"pr.debug",args:"initialized "+(u?"with PR":"without PR")}),u?o(u):l.createElement("div",{className:"loading-indicator"},"Loading...")}i(Kt,"Root"),addEventListener("load",Ge)})()})();
