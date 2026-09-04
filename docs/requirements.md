# Requirements Gathering Template

## Project Context
- **System Name**: Maintenance Request System (MRS)
- **Organization**: [University Name]
- **Department**: Works / Maintenance
- **Status**: Standalone (ZEGU integration future)

## Functional Requirements

### 1. User Management
- [ ] User registration (students, staff)
- [ ] Role-based access (Student, Staff, Works Officer, Technician, Manager, Admin)
- [ ] Password reset / account management

### 2. Request Submission
- [ ] Submit maintenance request
- [ ] Select category (electrical, plumbing, etc.)
- [ ] Select location (building → floor → room)
- [ ] Upload photos
- [ ] Provide description
- [ ] View request status
- [ ] Request history

### 3. Request Processing
- [ ] Works receives request
- [ ] Review/approve/reject
- [ ] Assign technician
- [ ] Update priority
- [ ] Add comments
- [ ] Record work performed
- [ ] Mark complete

### 4. Verification & Closure
- [ ] Requester confirms completion
- [ ] Works verifies
- [ ] Request closed

### 5. Notifications
- [ ] Status change notifications
- [ ] Assignment notifications
- [ ] Overdue alerts

### 6. Reporting
- [ ] Request statistics
- [ ] Technician performance
- [ ] Overdue requests
- [ ] Category/location breakdown

## Non-Functional Requirements

- [ ] Response time: [Define SLA targets]
- [ ] Concurrent users: [Estimate]
- [ ] Uptime requirement: [99.5%?]
- [ ] Data retention: [X years]
- [ ] Browser support: Chrome, Firefox, Edge

## Integration Requirements (Future)

- [ ] ZEGU ticketing system integration
- [ ] University student/staff database (LDAP/AD)
- [ ] Email/SMS gateway

## Constraints

- [ ] Budget: [TBD]
- [ ] Timeline: [TBD]
- [ ] Must work on mobile devices
- [ ] Must support offline request submission (future)
