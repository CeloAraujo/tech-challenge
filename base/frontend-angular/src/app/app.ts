import { Component } from '@angular/core';

import { PlanosLista } from './planos/planos-lista';
import { Beneficiarios } from './beneficiarios/beneficiarios';

@Component({
  selector: 'app-root',
  imports: [PlanosLista, Beneficiarios],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {}
