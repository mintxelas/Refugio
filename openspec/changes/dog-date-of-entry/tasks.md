## 1. Domain & Application layer

- [x] 1.1 Add `DateTime arrivalDate` parameter to `Dog.UpdateDetails` and assign `ArrivalDate = arrivalDate`
- [x] 1.2 Add `DateTime ArrivalDate` to `CreateDogRequest`
- [x] 1.3 Add `DateTime ArrivalDate` to `UpdateDogRequest`
- [x] 1.4 Update `DogService.CheckInDogAsync` to pass `request.ArrivalDate` to `Dog.CheckIn`
- [x] 1.5 Update `DogService.UpdateDogAsync` to pass `request.ArrivalDate` to `dog.UpdateDetails`

## 2. React SPA

- [x] 2.1 Add `arrivalDate` to the `create` and `update` payload in `ClientApp/src/api/dogs.ts`
- [x] 2.2 Add `arrivalDate` date input to `DogCheckin.tsx` (pre-filled with today, mandatory, validated)
- [x] 2.3 Add `arrivalDate` date input to `DogEdit.tsx` (loaded from DTO, mandatory, validated, submitted)
- [x] 2.4 Display `arrivalDate` in `DogDetail.tsx`

## 3. Tests

- [x] 3.1 Update any unit test that constructs `CreateDogRequest` or `UpdateDogRequest` to include `ArrivalDate`
- [x] 3.2 Add unit test: `CheckInDogAsync` persists the provided `ArrivalDate` (not `UtcNow`)
- [x] 3.3 Add unit test: `UpdateDogAsync` updates `ArrivalDate` to the new value
