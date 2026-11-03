# Repository development guidance

## Testing

- Before adding tests, inspect the existing test suite and extend or refine existing coverage instead of creating a duplicate test for behavior that is already covered.
- Use the existing `TestDataFixture` (`AutoFixture` customized with `AutoMoqCustomization`) for generated test data and dependency setup where appropriate. Keep contract assertions deterministic by explicitly setting values relevant to the behavior under test.
- Use AutoMoq to isolate external collaborators when a test needs them. Do not add mocks to pure logic tests that have no collaborators.
- Cover meaningful conditional branches, including boundary, omitted, and error paths where relevant.
- Check data integrity across boundaries: assert important input values, collection count/order, and output values survive mapping, serialization, and deserialization as intended.

## Design

- Prefer modular, swappable components when applicable, especially around I/O and data sources. Keep external services, file access, and persistence behind focused interfaces or boundaries so implementations can be replaced and behavior can be tested independently.
- Keep business logic separate from infrastructure details, and avoid introducing abstractions where there is no meaningful variation or test boundary.
