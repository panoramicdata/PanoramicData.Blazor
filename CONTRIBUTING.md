# Contributing

Thank you for your interest in contributing to this project!

## How to Contribute

1. **Fork** the repository
2. **Create a branch** for your feature or fix (`git checkout -b feature/my-feature`)
3. **Make your changes** following the coding standards below
4. **Write or update tests** as appropriate
5. **Ensure the build passes** with zero errors, zero warnings, and zero messages
6. **Submit a Pull Request** against the `main` branch

## Coding Standards

- All public members must have XML documentation comments
- Use `System.Text.Json` — do not introduce `Newtonsoft.Json`
- Use Refit for HTTP client interfaces
- Use file-scoped namespaces
- Use the `required` keyword for DTO properties where appropriate
- Ensure `TreatWarningsAsErrors` remains enabled
- All code must compile with zero diagnostics

## Testing

- Use xUnit v3 for all tests
- Use AwesomeAssertions for fluent assertions
- Ensure all existing tests pass before submitting a PR

## License

By contributing, you agree that your contributions will be licensed under the MIT License.