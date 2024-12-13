use regex::Regex;
use std::{fs, io};
use std::path::PathBuf;

use crate::structs::{YamlMatcher, Matcher};

pub fn load_matchers_from_file(file_path: &str) -> Result<(Vec<Matcher>, Vec<Matcher>), io::Error> {
    // Read the YAML file into a string
    let content = fs::read_to_string(file_path)?;
    
    // Parse the YAML into a Vec<YamlMatcher>
    let yaml_matchers: Vec<YamlMatcher> = serde_yaml::from_str(&content)
        .map_err(|e| io::Error::new(io::ErrorKind::InvalidData, e))?;
    
    // Separate matchers into blacklist and whitelist
    let mut blacklist = Vec::new();
    let mut whitelist = Vec::new();

    for yaml_matcher in yaml_matchers {
        let file_paths: Vec<PathBuf> = yaml_matcher
            .on_paths
            .into_iter()
            .map(PathBuf::from)
            .collect();
        
        for pattern_str in yaml_matcher.entity_matchers {
            // Wrap with "proto: " and "\n" to get only matches we care about for blacklist matchers.
            let full_pattern = match yaml_matcher.match_type.as_str() {
                "Blacklist" => String::from("proto: ") + &pattern_str + "\n",
                "Whitelist" => pattern_str.clone(), // This clone call is actually necessary, as the "original" pattern is preserved.
                _ => return Err(io::Error::new(
                    io::ErrorKind::InvalidData,
                    format!("Unknown match type: {}", yaml_matcher.match_type),
                )),
            };

            let pattern = Regex::new(&full_pattern).map_err(|e| io::Error::new(io::ErrorKind::InvalidData, e))?;
            let matcher = Matcher {
                pattern,
                original_pattern: pattern_str,
                matcher_block_id: yaml_matcher.id.clone(), // Really don't like having to use clone here. Fix another day.
                file_paths: file_paths.clone()
            };

            match yaml_matcher.match_type.as_str() {
                "Blacklist" => blacklist.push(matcher),
                "Whitelist" => whitelist.push(matcher),
                _ => return Err(io::Error::new( // Have to repeat this cause otherwise rust complains, but this is NEVER hit.
                    io::ErrorKind::InvalidData,
                    format!("Unknown match type: {}", yaml_matcher.match_type),
                )),
            }
        }
    }
    
    Ok((blacklist, whitelist))
}