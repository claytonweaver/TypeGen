// Code generated by jtd-codegen for Rust v0.2.1

use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize)]
pub enum IncomePassiveIncomePassiveIncomeType {
    #[serde(rename = "Investment")]
    Investment,

    #[serde(rename = "Rental")]
    Rental,

    #[serde(rename = "Royalties")]
    Royalties,
}