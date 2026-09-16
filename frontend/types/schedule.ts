export interface Schedule {
  id: string;
  staffId: string;
  staffName: string;
  workDate: string;
  startTime: string;
  endTime: string;
}

export interface CreateScheduleRequest {
  workDate: string;
  startTime: string;
  endTime: string;
}

export interface UpdateScheduleRequest {
  workDate: string;
  startTime: string;
  endTime: string;
}
