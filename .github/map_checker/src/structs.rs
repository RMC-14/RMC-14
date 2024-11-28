use regex::Regex;
use std::path::PathBuf;
use serde::Deserialize;

#[derive(Deserialize)]
pub struct YamlMatcher {
    /// Defines a matcher block as defined in the matcher configuration.
    #[serde(rename = "id")] // ID for debugging a match and knowing where it came from.
    pub id: String,
    #[serde(rename = "type")] // Blacklist or Whitelist
    pub match_type: String,
    #[serde(rename = "entityMatchers")] // Set of regex patterns.
    pub entity_matchers: Vec<String>,
    #[serde(rename = "on_paths", default)] // Paths to restrict this matcher block to. Default: All paths.
    pub on_paths: Vec<String>,
}

pub struct Matcher {
    pub pattern: Regex,
    pub original_pattern: String,
    pub matcher_block_id: String,
    pub file_paths: Vec<PathBuf>
}

pub struct Match {
    pub matched_text: String,
    pub info: String
}