terraform {
  backend "s3" {
    bucket       = "secret-nick-santa-terraform-2025marathon"
    key          = "terraform.tfstate"
    region       = "eu-central-1"
    use_lockfile = true
    encrypt      = true
  }
}
