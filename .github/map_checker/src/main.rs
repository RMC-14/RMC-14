use log::{info, error};
use clap::Parser;
use simple_logger::SimpleLogger;
use std::process;
use walkdir::WalkDir;

mod structs;

mod scanfile;
use scanfile::scan_file;

mod loadmatchers;
use loadmatchers::load_matchers_from_file;

#[derive(Parser, Debug)]
#[command(version, about, long_about = None)]
struct Args {
    /// Matcher config file location
    #[arg(short, long, default_value_t = String::from("matchers.yml"))]
    config_file: String,

    /// Map file directory location
    #[arg(short, long, default_value_t = String::from("../../Resources/Maps/_RMC14"))]
    maps_directory: String,
}

fn main() {
    // Set up logger and argument parser.
    SimpleLogger::new().env().init().unwrap();
    let arguments = Args::parse();

    // Get blacklist and whitelist from matcher config files.
    let (blacklist, whitelist) = load_matchers_from_file(&arguments.config_file)
        .unwrap_or_else(|e| panic!("Failed to load configuration file: {}, Error: {}", arguments.config_file, e));

    // Result variable.
    let mut found_errors = false;

    // Enumerate maps in the target directory. Scan each one.
    for entry in WalkDir::new(arguments.maps_directory).into_iter().filter_map(Result::ok) {
        let path = entry.path();

        if path.is_file() {
            // Actual file scanning happens here.
            let matches = scan_file(path, &blacklist, &whitelist)
                .unwrap_or_else(|e| panic!("Failed to load map file: {}, Error: {}", path.display(), e));

            // Handle matches found if any. Set found_errors flag.
            if !matches.is_empty() {
                found_errors = true;

                info!("Found blacklisted prototypes in file: {}", path.display());
                for m in matches {
                    info!(" - {} ({})", m.matched_text, m.info);
                }
            }
        }
    }
    
    // Exit.
    if found_errors {
        error!("Mapchecker ran successfully and found errors. Exiting.");
        process::exit(1)
    } else {
        info!("Mapchecker ran successfully and found no errors. Exiting.");
        process::exit(0)
    }
}

