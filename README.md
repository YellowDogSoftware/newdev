# newdev
- i estimate this should take no longer than 1-2 hours. 
- the purpose of this is to present general ETL ability in .net. I expect most of the concepts used here will be very familiar.
## Scope 
- .net6 console app 
- use any thirdparty packages you wish
- http://restapi.adequateshop.com api (this is the api that will be used for this application, documentation can be found at https://www.appsloveworld.com/sample-rest-api-url-for-testing-with-authentication)
- need to allow a user to input an email and password (simple command line entry is fine)
- register or log user into api and save token for later calls
- save user information to sqlite database (simple db file with a single table is fine, but a sql db must be used ,must include name, email, and createdat at minimum)
- call the /users endpoint, page through responses and save these records to the sqlite db table
- query the db table of users and export this data to a csv file that is saved locally
- should be generally idempotent and cover basic error handling (no need to cover every possible situation, but if a password does not meet the apis expectation, i would expect this to be displayed to the user rather than to simply crash)
- This repo should be used to submit a PR of the code for review. 
