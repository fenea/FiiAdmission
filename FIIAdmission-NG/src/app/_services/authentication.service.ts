import { Injectable } from '@angular/core';
import { Http, Headers, Response } from '@angular/http';
import { Observable } from 'rxjs/Observable';
import 'rxjs/add/operator/map';
import { AppConfig } from '../app-config';
import { error } from 'util';


@Injectable()
export class Authentication {
  constructor(private http: Http, private config: AppConfig) { }
  login(userName: string, password: string) {
    const res = this.http.post(this.config.apiUrl + '/api/Auth/login', { userName: userName, password: password });
    res.subscribe(
      data => {
        localStorage.setItem('currentUser', JSON.stringify(data));
        localStorage.setItem('currUserMail', JSON.parse(JSON.parse(JSON.stringify(data))._body).email);
        localStorage.setItem('auth_token', JSON.parse(JSON.parse(JSON.parse(JSON.stringify(data))._body).token).auth_token);
        const r = this.getDashmin();
        r.subscribe(
          // tslint:disable-next-line:no-shadowed-variable
          data => {
            localStorage.setItem('admin', JSON.stringify(data.status));
          },
          // tslint:disable-next-line:no-shadowed-variable
          error => {
            localStorage.setItem('admin', JSON.stringify(error.status));
          }
        );
      },
    );
    return res;
  }

  getDashmin() {
    const headers = new Headers();
    headers.append('Content-Type', 'application/json');
    const authToken = localStorage.getItem('auth_token');
    headers.append('Authorization', `Bearer ${authToken}`);
    return this.http.get(this.config.apiUrl + '/api/Account/admin', { headers });
  }

  logout() {
    localStorage.removeItem('admin');
    localStorage.removeItem('currentUser');
    localStorage.removeItem('currUserMail');
    localStorage.removeItem('auth_token');
  }
}
